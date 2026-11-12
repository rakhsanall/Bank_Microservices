using CustomerService.Data;
using CustomerService.DTOs;
using CustomerService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CustomerService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IWebHostEnvironment _environment;

        public CustomersController(
     CustomerDbContext context,
     IWebHostEnvironment environment,
     IDistributedCache cache)
        {
            _context = context;
            _environment = environment;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateCustomerDto dto)
        {
            string email = dto.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Customers
                .AnyAsync(x => x.Email == email);

            if (emailExists)
                return Conflict("Email already exists.");

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = email,
                PhoneNumber = dto.PhoneNumber
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = customer.Id },
                customer);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            string cacheKey = $"customer:{id}";

            string? cachedCustomer = await _cache.GetStringAsync(cacheKey);

            if (cachedCustomer != null)
            {
                return Content(
                    cachedCustomer,
                    "application/json");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return NotFound();

            string json = System.Text.Json.JsonSerializer.Serialize(customer);

            await _cache.SetStringAsync(
                cacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(10)
                });

            return Ok(customer);
        }

        [HttpPost("{id:guid}/photo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return NotFound("Customer not found.");

            if (file == null || file.Length == 0)
                return BadRequest("Please select an image.");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Maximum image size is 5 MB.");

            string extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                return BadRequest("Only JPG and PNG images are allowed.");
            }

            if (!await HasValidImageSignatureAsync(file, extension))
                return BadRequest("Invalid image file content.");

            string folder = Path.Combine(
                _environment.ContentRootPath,
                "Uploads",
                "CustomerPhotos");

            Directory.CreateDirectory(folder);

            string fileName = $"{Guid.NewGuid():N}{extension}";
            string filePath = Path.Combine(folder, fileName);

            string? oldFileName = customer.ProfilePhotoUrl;

            await using (var stream = new FileStream(
                filePath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                customer.ProfilePhotoUrl = fileName;

                await _context.SaveChangesAsync();

                await _cache.RemoveAsync($"customer:{id}");
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                throw;
            }

            if (!string.IsNullOrWhiteSpace(oldFileName))
            {
                string safeOldName = Path.GetFileName(oldFileName);
                string oldPath = Path.Combine(folder, safeOldName);

                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            return Ok(new
            {
                customer.Id,
                PhotoUrl = Url.Action(
                    nameof(GetPhoto),
                    "Customers",
                    new { id })
            });
        }

        private static async Task<bool> HasValidImageSignatureAsync(
        IFormFile file,
        string extension)
        {
            byte[] header = new byte[8];

            await using var stream = file.OpenReadStream();

            int totalRead = 0;

            while (totalRead < header.Length)
            {
                int read = await stream.ReadAsync(
                    header.AsMemory(totalRead));

                if (read == 0)
                    break;

                totalRead += read;
            }

            if (extension == ".jpg" || extension == ".jpeg")
            {
                return totalRead >= 3 &&
                       header[0] == 0xFF &&
                       header[1] == 0xD8 &&
                       header[2] == 0xFF;
            }

            if (extension == ".png")
            {
                byte[] pngSignature =
                {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

                return totalRead == 8 &&
                       header.SequenceEqual(pngSignature);
            }

            return false;
        }

        [HttpGet("{id:guid}/photo")]
        public async Task<IActionResult> GetPhoto(Guid id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return NotFound("Customer not found.");

            if (string.IsNullOrWhiteSpace(customer.ProfilePhotoUrl))
                return NotFound("Customer has no profile photo.");

            string fileName = Path.GetFileName(customer.ProfilePhotoUrl);

            if (fileName != customer.ProfilePhotoUrl)
                return BadRequest("Invalid file path.");

            string folder = Path.Combine(
                _environment.ContentRootPath,
                "Uploads",
                "CustomerPhotos");

            string path = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(path))
                return NotFound("Photo file not found.");

            string contentType =
                Path.GetExtension(fileName).ToLowerInvariant() switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };

            return PhysicalFile(path, contentType);
        }

    }
}