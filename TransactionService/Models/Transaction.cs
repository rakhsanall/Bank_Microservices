namespace TransactionService.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CustomerId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "AZN";

        public string Status { get; set; } = "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}