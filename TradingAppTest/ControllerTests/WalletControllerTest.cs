using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TradingApp.API.Controllers;
using TradingApp.API.Models;
using TradingApp.API.Models.DTO;
using TradingApp.API.Services;

namespace TradingAppTest.ControllerTests
{
    [TestClass]
    public class WalletControllerTest
    {
        private ApplicationDbContext _context;
        private WalletService _walletService;
        private BlockchainService _blockchainService;
        private WalletController _controller;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            TestDbInitializer.Initialize(_context);

            _walletService = new WalletService(_context);

            _controller = new WalletController(_walletService, _blockchainService);
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
        public async Task GetBalances_ReturnsOkResultWithBalances()
        {
            // Act
            var result = await _controller.GetBalances();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var balances = okResult.Value as List<Balance>;
            Assert.IsNotNull(balances);
            Assert.AreEqual(3, balances.Count);

            var ethBalance = balances.FirstOrDefault(b => b.Currency == "ETH");
            var btcBalance = balances.FirstOrDefault(b => b.Currency == "BTC");

            Assert.IsNotNull(ethBalance);
            Assert.IsNotNull(btcBalance);

            Assert.AreEqual(10m, ethBalance.Amount);
            Assert.AreEqual(0.5m, btcBalance.Amount);
        }

        [TestMethod]
        public async Task GetPendingTransactions_ReturnsOkResultWithPendingTransactions()
        {
            // Act
            var result = await _controller.GetPendingTransactions();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var transactions = okResult.Value as List<Transaction>;
            Assert.IsNotNull(transactions);
            Assert.AreEqual(1, transactions.Count);
            Assert.IsTrue(transactions.All(t => t.IsPending));
        }

        [TestMethod]
        public async Task GetTransactions_ReturnsOkResultWithTransactionHistory()
        {
            // Act
            var result = await _controller.GetTransactions();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var transactions = okResult.Value as List<Transaction>;
            Assert.IsNotNull(transactions);
            Assert.AreEqual(1, transactions.Count);
            Assert.IsTrue(transactions.All(t => !t.IsPending));
        }

        [TestMethod]
        public async Task DepositFunds_ReturnsOkResultWithMessage()
        {
            // Arrange
            var depositDto = new DepositDto
            {
                Currency = "BTC",
                Amount = 0.1m
            };

            // Act
            var result = await _controller.DepositFunds(depositDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var wallet = await _context.Wallets.Include(w => w.Balances).FirstOrDefaultAsync(w => w.UserId == 1);
            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == "BTC");
            Assert.IsNotNull(balance);
            Assert.AreEqual(0.6m, balance.Amount);

            var transaction = wallet.Transactions.FirstOrDefault(t => t.Type == "Deposit" && t.Currency == "BTC" && t.Amount == 0.1m);
            Assert.IsNotNull(transaction);
            Assert.AreEqual("Deposit", transaction.Type);
            Assert.AreEqual(0.1m, transaction.Amount);
            Assert.AreEqual("address1", transaction.From);
            Assert.AreEqual("sampleAddress", transaction.To);
            Assert.AreEqual(1, transaction.UserId);
        }

        [TestMethod]
        public async Task WithdrawFunds_ReturnsOkResultWithMessage()
        {
            // Arrange
            var withdrawDto = new DepositDto
            {
                Currency = "BTC",
                Amount = 0.2m
            };

            // Act
            var result = await _controller.WithdrawFunds(withdrawDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var wallet = await _context.Wallets.Include(w => w.Balances).FirstOrDefaultAsync(w => w.UserId == 1);
            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == "BTC");
            Assert.IsNotNull(balance);
            Assert.AreEqual(0.3m, balance.Amount);

            var transaction = wallet.Transactions.FirstOrDefault(t => t.Type == "Withdrawal" && t.Currency == "BTC" && t.Amount == 0.2m);
            Assert.IsNotNull(transaction);
            Assert.AreEqual("Withdrawal", transaction.Type);
            Assert.AreEqual(0.2m, transaction.Amount);
            Assert.AreEqual("own", transaction.From);
            Assert.AreEqual("own", transaction.To);
            Assert.IsFalse(transaction.IsPending);
            Assert.AreEqual(1, transaction.UserId);
        }

    }
}
