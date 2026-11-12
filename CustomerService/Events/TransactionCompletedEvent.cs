namespace CustomerService.Events
{
    public class TransactionCompletedEvent
    {
        public Guid TransactionId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "AZN";
        public DateTime OccurredAt { get; set; }
    }
}