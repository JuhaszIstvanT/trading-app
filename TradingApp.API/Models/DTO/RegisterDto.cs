using System.ComponentModel.DataAnnotations;

namespace TradingApp.API.Models.DTO
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string DebitCardNumber { get; set; }
    }
}
