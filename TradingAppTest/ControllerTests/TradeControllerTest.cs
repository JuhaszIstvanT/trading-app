using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingApp.API.Controllers;
using TradingApp.API.Models;
using TradingApp.API.Models.DTO;
using TradingApp.API.Services;

namespace TradingAppTest.ControllerTests
{
    [TestClass]
    public class TradeControllerTest
    {
        private TradeService _tradeService;
        private TradeController _controller;
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

            _controller = new TradeController(_tradeService);
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
        public async Task GetTrades_ReturnsOkResultWithActiveTrades()
        {
            // Act
            var result = await _controller.GetTrades();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            var trades = okResult.Value as List<Trade>;
            Assert.IsNotNull(trades);
            Assert.AreEqual(2, trades.Count);
        }

        [TestMethod]
        public async Task GetTradehistory_ReturnsOkResultWithSoldTrades()
        {
            // Act
            var result = await _controller.GetTradehistory();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            var soldTrades = okResult.Value as List<Trade>;
            Assert.IsNotNull(soldTrades);
            Assert.AreEqual(1, soldTrades.Count);
        }

        [TestMethod]
        public async Task PlaceStopLossOrder_ReturnsOkResultWhenOrderIsSet()
        {
            // Arrange
            var tradeId = 1;
            var orderDto = new OrderDto { Price = 500 };

            // Act
            var result = await _controller.PlaceStopLossOrder(tradeId, orderDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var trade = await _context.Trades.FindAsync(tradeId);
            Assert.IsNotNull(trade);
            Assert.AreEqual(orderDto.Price, trade.StopLossOrderPrice);
            Assert.IsTrue(trade.StopLossOrderActive);
        }

        [TestMethod]
        public async Task PlaceStopLossOrder_ReturnsNotFoundWhenTradeNotFound()
        {
            // Arrange
            var nonExistingTradeId = 999;
            var orderDto = new OrderDto { Price = 500 };

            // Act
            var result = await _controller.PlaceStopLossOrder(nonExistingTradeId, orderDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            var notFoundResult = result as NotFoundResult;
            Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [TestMethod]
        public async Task Buy_ReturnsOkResultWhenTradeIsSuccessful()
        {
            // Arrange
            var tradeDto = new TradeDto
            {
                PaymentCurrency = "USD",
                PaymentAmount = 100,
                CurrencyToBuy = "BTC",
                Name = "Bitcoin",
                AmountToBuy = 0.01m
            };

            // Act
            var result = await _controller.Buy(tradeDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var userId = Convert.ToInt32("1");
            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Trades)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            Assert.IsNotNull(wallet);
            Assert.IsTrue(wallet.Trades.Any());
            decimal balanceUSD = wallet.Balances.FirstOrDefault(b => b.Currency == "USD").Amount;
            Assert.AreEqual(400, balanceUSD);
        }

        [TestMethod]
        public async Task Sell_ReturnsOkResultWhenTradeIsSuccessful()
        {
            // Arrange
            var tradeDto = new TradeDto
            {
                PaymentCurrency = "USD",
                PaymentAmount = 100,
                CurrencyToBuy = "BTC",
                Name = "Bitcoin",
                AmountToBuy = 0.01m,
                Id = 1
            };

            // Act
            var result = await _controller.Sell(tradeDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var userId = Convert.ToInt32("1");
            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Trades)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            Assert.IsNotNull(wallet);
            var soldTrade = wallet.Trades.FirstOrDefault(t => t.Id == tradeDto.Id);
            Assert.IsNotNull(soldTrade);
            Assert.IsTrue(soldTrade.IsSold);
            decimal balanceUSD = wallet.Balances.FirstOrDefault(b => b.Currency == "USD").Amount;
            Assert.AreEqual(600, balanceUSD);
        }
    }

}
