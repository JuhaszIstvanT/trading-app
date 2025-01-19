using System.ComponentModel.DataAnnotations;

namespace TradingApp.API.Models
{
    public class Balance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Currency { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }
    }
}
