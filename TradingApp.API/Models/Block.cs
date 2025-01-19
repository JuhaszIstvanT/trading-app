using TradingApp.API.Models;

namespace TradingApp.API.Models
{
    public class Block
    {
        public Block()
        {
            Transactions = new List<Transaction>();
        }

        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string PreviousHash { get; set; }
        public int Height { get; set; }
        public int Nonce { get; set; }

        public List<Transaction> Transactions { get; set; }
    }
}