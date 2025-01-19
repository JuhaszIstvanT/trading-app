namespace TradingApp.API.Models.DTO
{
    public class TraderDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal Profit { get; set; }
        public Wallet Wallet { get; set; }
    }
}
