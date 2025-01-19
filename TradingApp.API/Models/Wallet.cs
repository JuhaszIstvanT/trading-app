using System.ComponentModel.DataAnnotations;

namespace TradingApp.API.Models
{
    public class Wallet
    {
        public Wallet()
        {
            Balances = new List<Balance>();
            Transactions = new List<Transaction>();
            Trades = new List<Trade>();
            Watchlist = new List<string>();
        }
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        [Required]
        public List<Balance> Balances { get; set; }
        public List<Transaction> Transactions { get; set; }
        public List<Trade> Trades { get; set; }
        public List<string> Watchlist { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string Address { get; set; }
    }
}
