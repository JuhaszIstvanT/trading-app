namespace TradingApp.API.Models.DTO
{
    public class TransferDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
