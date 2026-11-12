using System.ComponentModel.DataAnnotations;

namespace TransactionService.DTOs
{
    public class CreateTransactionDto
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Range(typeof(decimal), "0.01", "999999999999.99")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression("^(AZN|USD|EUR)$")]
        public string Currency { get; set; } = "AZN";
    }
}