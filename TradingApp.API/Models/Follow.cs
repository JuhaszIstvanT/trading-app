namespace TradingApp.API.Models
{
    public class Follow
    {
        public int Id { get; set; }
        public int FollowerId { get; set; }
        public int FollowedTraderId { get; set; }
        public DateTime FollowedOn { get; set; }
    }
}
