using Microsoft.AspNetCore.Mvc;
using TradingApp.API.Services;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketController : ControllerBase
    {
        private readonly MarketService _marketService;

        public MarketController(MarketService marketService)
        {
            _marketService = marketService;
        }

        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalMarketData()
        {
            try
            {
                var jsonData = await _marketService.GetGlobalMarketDataAsync();
                return Content(jsonData, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("tickers")]
        public async Task<IActionResult> GetTickerData()
        {
            try
            {
                var data = await _marketService.GetTickerDataAsync();
                return Content(data, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("ticker")]
        public async Task<IActionResult> GetSpecificTickerData([FromQuery] string id)
        {
            try
            {
                var data = await _marketService.GetSpecificTickerDataAsync(id);
                return Content(data, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
