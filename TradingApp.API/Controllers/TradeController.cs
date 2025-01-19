using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingApp.API.Models.DTO;
using TradingApp.API.Services;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TradeController : ControllerBase
    {
        private readonly TradeService _tradeService;

        public TradeController(TradeService tradeService)
        {
            _tradeService = tradeService;
        }

        [HttpGet("trades")]
        public async Task<IActionResult> GetTrades()
        {
            try
            {
                var wallet = await _tradeService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                var activeTrades = wallet.Trades.Where(trade => !trade.IsSold).ToList();

                return Ok(activeTrades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet transactions.");
            }
        }

        [HttpGet("tradehistory")]
        public async Task<IActionResult> GetTradehistory()
        {
            try
            {
                var wallet = await _tradeService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                var soldTrades = wallet.Trades.Where(trade => trade.IsSold).ToList();

                return Ok(soldTrades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet transactions.");
            }
        }

        [HttpGet("trade/{id}")]
        public async Task<IActionResult> GetTradeDetails(int id)
        {
            var trade = await _tradeService.GetTradeDetailsAsync(id);

            if (trade == null)
            {
                return NotFound();
            }

            return Ok(trade);
        }

        [HttpPost("trade/{id}/stoplossorder")]
        public async Task<IActionResult> PlaceStopLossOrder(int id, OrderDto orderDto)
        {
            bool isSet = await _tradeService.SetStopLossOrderPrice(id, orderDto.Price);
            if (isSet == false)
            {
                return NotFound();
            }

            return Ok(new { message = "Stoplossorder successful" });
        }

        [HttpPost("buy")]
        public async Task<IActionResult> Buy([FromBody] TradeDto tradeDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _tradeService.BuyAsync(Convert.ToInt32(userId), tradeDto);
            if (result)
            {
                return Ok(new { message = "Trade successful" });
            }
            else
            {
                return BadRequest(new { message = "Trade wasn't successful" });
            }
        }

        [HttpPost("sell")]
        public async Task<IActionResult> Sell([FromBody] TradeDto tradeDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _tradeService.SellAsync(Convert.ToInt32(userId), tradeDto);
            if (result)
            {
                return Ok(new { message = "Trade successful" });
            }
            else
            {
                return Ok(new { message = "Trade wasn't successful" });
            }
        }

        [HttpGet("traders")]
        public  async Task<IActionResult> GetTopTraders(int count = 25)
        {
            var topTraders = await _tradeService.GetTopTraders(count);
            return Ok(topTraders);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var trader = await _tradeService.GetTrader(id);
            return Ok(trader);
        }
    }
}
