using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.API.Models.DTO;
using TradingApp.API.Services;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WatchlistController : ControllerBase
    {
        private readonly TradeService _tradeService;

        public WatchlistController(TradeService tradeService)
        {
            _tradeService = tradeService;
        }

        [HttpGet("currencies")]
        public async Task<IActionResult> GetCurrencies()
        {
            try
            {
                var wallet = await _tradeService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                return Ok(wallet.Watchlist);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet balances.");
            }
        }

        [HttpDelete("remove/{currency}")]
        public async Task<IActionResult> RemoveCurrency(string currency)
        {
            try
            {
                var wallet = await _tradeService.GetWalletForCurrentUserAsync(User);

                await _tradeService.RemoveCurrencyFromWatchlistAsync(wallet, currency);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while removing the currency from the watchlist.");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCurrency([FromBody] WatchlistDto watchlistDto)
        {
            try
            {
                var wallet = await _tradeService.GetWalletForCurrentUserAsync(User);

                await _tradeService.AddCurrencyToWatchlistAsync(wallet, watchlistDto.Symbol);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while adding the currency to the watchlist.");
            }
        }
    }
}
