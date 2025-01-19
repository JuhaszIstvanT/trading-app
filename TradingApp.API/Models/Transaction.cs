using System.ComponentModel.DataAnnotations;

namespace TradingApp.API.Models
{
    public class Transaction
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Type { get; set; } = null!;

        [Required]
        public string Currency { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        public decimal TransactionFee { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public bool IsPending { get; set; }

        public bool IsValid { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
