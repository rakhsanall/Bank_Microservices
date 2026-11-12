using LoanService.Data;
using LoanService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;


namespace LoanService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanDocumentsController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LoanDocumentsController(
            LoanDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("{customerId:guid}/upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
     Guid customerId,
      IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please select a document.");

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("Maximum document size is 10 MB.");

            string extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (extension != ".pdf" &&
                extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                return BadRequest("Unsupported file format.");
            }

            if (!await HasValidDocumentSignatureAsync(file, extension))
                return BadRequest("Invalid document content.");

            string folder = Path.Combine(
                _environment.ContentRootPath,
                "LoanUploads");

            Directory.CreateDirectory(folder);

            string storedFileName = $"{Guid.NewGuid()}{extension}";
            string path = Path.Combine(folder, storedFileName);

            await using (var stream = new FileStream(
                path, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var document = new LoanDocument
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                FileName = Path.GetFileName(file.FileName),
                FilePath = storedFileName,
                UploadedAt = DateTime.UtcNow
            };

            _context.LoanDocuments.Add(document);
            await _context.SaveChangesAsync();

            return Ok(document);
        }



        [HttpGet("{customerId:guid}")]
        public async Task<IActionResult> GetDocuments(Guid customerId)
        {
            var documents = await _context.LoanDocuments
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .ToListAsync();

            return Ok(documents);
        }
        [HttpGet("document/{documentId:guid}/download")]
        public async Task<IActionResult> Download(Guid documentId)
        {
            var document = await _context.LoanDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == documentId);

            if (document == null)
                return NotFound("Document not found.");

            string folder = Path.Combine(
                _environment.ContentRootPath,
                "LoanUploads");

            string path = Path.Combine(folder, document.FilePath);

            if (!System.IO.File.Exists(path))
                return NotFound("File not found.");

            string contentType = Path.GetExtension(document.FileName)
                .ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return File(stream, contentType, document.FileName);
        }

        private static async Task<bool> HasValidDocumentSignatureAsync(
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

            if (extension == ".pdf")
            {
                return totalRead >= 5 &&
                       Encoding.ASCII.GetString(header, 0, 5) == "%PDF-";
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
    }
}