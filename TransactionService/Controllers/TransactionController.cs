using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.DTOs;
using TransactionService.Events;
using TransactionService.Models;
using TransactionService.Services;

namespace TransactionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionDbContext _context;
        private readonly RabbitMqPublisher _publisher;

        public TransactionsController(
            TransactionDbContext context,
            RabbitMqPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTransactionDto dto)
        {
            if (dto.CustomerId == Guid.Empty)
                return BadRequest("Valid Customer ID is required.");

            if (dto.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            string currency = dto.Currency.Trim().ToUpperInvariant();

            if (currency != "AZN" &&
                currency != "USD" &&
                currency != "EUR")
            {
                return BadRequest("Unsupported currency.");
            }
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                Currency = currency,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _publisher.Publish(new TransactionCompletedEvent
            {
                TransactionId = transaction.Id,
                CustomerId = transaction.CustomerId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                OccurredAt = transaction.CreatedAt
            });

            return CreatedAtAction(
                nameof(GetById),
                new { id = transaction.Id },
                transaction);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return transaction == null
                ? NotFound()
                : Ok(transaction);
        }

        [HttpGet("customer/{customerId:guid}")]
        public async Task<IActionResult> GetByCustomer(Guid customerId)
        {
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(transactions);
        }
    }
}