using Microsoft.AspNetCore.Identity;

namespace TradingApp.API.Models
{
    public class User : IdentityUser<int>
    {
        public string DebitCardNumber { get; set; }
    }
}
