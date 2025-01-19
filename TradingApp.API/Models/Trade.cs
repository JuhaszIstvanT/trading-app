namespace TradingApp.API.Models
{
    public class Trade
    {
        public int Id { get; set; }
        public decimal BuyPrice { get; set; }
        public string BuyCurrency { get; set; }
        public decimal SellPrice { get; set; }
        public string SellCurrency { get; set; } = string.Empty;
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public bool IsSold { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal StopLossOrderPrice { get; set; }
        public bool StopLossOrderActive { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}