namespace TradingApp.API.Models.DTO
{
    public class TradeDto
    {
        public int Id { get; set; }
        public string CurrencyToBuy { get; set; }
        public decimal AmountToBuy { get; set; }
        public string PaymentCurrency { get; set; }
        public decimal PaymentAmount { get; set; }
        public string Name { get; set; }
    }
}
