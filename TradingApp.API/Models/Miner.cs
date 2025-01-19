using System.ComponentModel.DataAnnotations;
using TradingApp.API.Models;

namespace TradingApp.API.Models
{
    public class Miner
    {
        [Key]
        public int Id { get; set; }
        public decimal Balance { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
    }
}