using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TradingApp.API.Controllers;
using TradingApp.API.Models.DTO;
using TradingApp.API.Models;
using TradingApp.API.Services;

namespace TradingAppTest.ControllerTests
{
    [TestClass]
    public class WatchlistControllerTest
    {
        private TradeService _tradeService;
        private WatchlistController _controller;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            TestDbInitializer.Initialize(_context);

            _tradeService = new TradeService(_context);

            _controller = new WatchlistController(_tradeService);
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Name, "testuser")
                    }, "mock"))
                }
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task GetCurrencies_ReturnsOkResultWithWatchlist()
        {
            // Act
            var result = await _controller.GetCurrencies();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult.Value);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var watchlist = okResult.Value as List<string>;
            Assert.IsNotNull(watchlist);
            Assert.AreEqual(2, watchlist.Count);
        }

        [TestMethod]
        public async Task RemoveCurrency_ReturnsOkResult()
        {
            // Arrange
            var currencyToRemove = "BTC";

            // Act
            var result = await _controller.RemoveCurrency(currencyToRemove);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            var okResult = result as OkResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var newResult = await _controller.GetCurrencies();
            var newOkResult = newResult as OkObjectResult;
            var newWatchlist = newOkResult.Value as List<string>;
            Assert.AreEqual(1, newWatchlist.Count);
        }

        [TestMethod]
        public async Task AddCurrency_ReturnsOkResult()
        {
            // Arrange
            var watchlistDto = new WatchlistDto { Symbol = "SOL" };

            // Act
            var result = await _controller.AddCurrency(watchlistDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            var okResult = result as OkResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var getResult = await _controller.GetCurrencies();
            var okObjectResult = getResult as OkObjectResult;
            var watchlist = okObjectResult.Value as List<string>;
            Assert.IsNotNull(watchlist);
            Assert.IsTrue(watchlist.Contains("SOL"));
            Assert.AreEqual(3, watchlist.Count);
        }
    }
}
