using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingApp.API.Models.DTO;
using TradingApp.API.Models;
using TradingApp.API.Services;

namespace TradingAppTest
{
    [TestClass]
    public class TradeServiceTest
    {
        private TradeService _tradeService;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _tradeService = new TradeService(_context);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            if (!_context.Wallets.Any())
            {
                var wallets = new List<Wallet>
        {
            new Wallet
            {
                UserId = 1,
                Address = "address1",
                PrivateKey = "privateKey1",
                PublicKey = "publicKey1",
                Balances = new List<Balance>
                {
                    new Balance { Currency = "USD", Amount = 200m },
                    new Balance { Currency = "BTC", Amount = 0.01m }
                },
                Watchlist = new List<string> { "BTC", "ETH", "XRP" },
                Trades = new List<Trade>
                {
                    new Trade
                    {
                        Id = 1,
                        BuyPrice = 50m,
                        BuyCurrency = "USD",
                        Symbol = "BTC",
                        Name = "Bitcoin",
                        Amount = 0.01m,
                        Timestamp = DateTime.UtcNow,
                        IsSold = true,
                        SellPrice = 150m,
                        UserId = 1,
                        StopLossOrderActive = false
                    },
                    new Trade
                    {
                        Id = 3,
                        BuyPrice = 200m,
                        BuyCurrency = "USD",
                        Symbol = "BTC",
                        Name = "Bitcoin",
                        Amount = 0.02m,
                        Timestamp = DateTime.UtcNow,
                        IsSold = true,
                        SellPrice = 300m,
                        UserId = 1,
                        StopLossOrderActive = false
                    }
                }
            },
            new Wallet
            {
                UserId = 2,
                Address = "address2",
                PrivateKey = "privateKey2",
                PublicKey = "publicKey2",
                Balances = new List<Balance>
                {
                    new Balance { Currency = "USD", Amount = 100m }
                },
                Watchlist = new List<string> { "LTC", "BNB" },
                Trades = new List<Trade>
                {
                    new Trade
                    {
                        Id = 2,
                        BuyPrice = 100m,
                        BuyCurrency = "USD",
                        Symbol = "BTC",
                        Name = "Bitcoin",
                        Amount = 0.01m,
                        Timestamp = DateTime.UtcNow,
                        IsSold = true,
                        SellPrice = 250m,
                        UserId = 2,
                        StopLossOrderActive = false
                    },
                    new Trade
                    {
                        Id = 4,
                        BuyPrice = 120m,
                        BuyCurrency = "USD",
                        Symbol = "ETH",
                        Name = "Ethereum",
                        Amount = 0.5m,
                        Timestamp = DateTime.UtcNow,
                        IsSold = false,
                        UserId = 2,
                        StopLossOrderActive = false
                    }
                }
            }
        };

                _context.Wallets.AddRange(wallets);
                _context.SaveChanges();
            }
        }


        [TestMethod]
        public async Task BuyAsync_Successful()
        {
            // Arrange
            int userId = 1;
            var tradeDto = new TradeDto
            {
                PaymentCurrency = "USD",
                PaymentAmount = 50m,
                CurrencyToBuy = "BTC",
                AmountToBuy = 0.005m,
                Name = "Bitcoin"
            };

            // Act
            var result = await _tradeService.BuyAsync(userId, tradeDto);

            // Assert
            Assert.IsTrue(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Trades)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == "USD");
            Assert.AreEqual(150m, balance.Amount);

            var btcBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "BTC");
            Assert.IsNotNull(btcBalance);
            Assert.AreEqual(0.015m, btcBalance.Amount);

            Assert.AreEqual(3, wallet.Trades.Count);
            var trade = wallet.Trades.Last();
            Assert.AreEqual("USD", trade.BuyCurrency);
            Assert.AreEqual(50m, trade.BuyPrice);
            Assert.AreEqual("BTC", trade.Symbol);
            Assert.AreEqual(0.005m, trade.Amount);
        }

        [TestMethod]
        public async Task BuyAsync_InsufficientBalance()
        {
            // Arrange
            int userId = 1;
            var tradeDto = new TradeDto
            {
                PaymentCurrency = "USD",
                PaymentAmount = 250m,
                CurrencyToBuy = "ETH",
                AmountToBuy = 0.01m,
                Name = "Ethereum"
            };

            // Act
            var result = await _tradeService.BuyAsync(userId, tradeDto);

            // Assert
            Assert.IsFalse(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == "USD");
            Assert.AreEqual(200m, balance.Amount);

            var ethBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "ETH");
            Assert.IsNull(ethBalance);
        }

        [TestMethod]
        public async Task BuyAsync_AddsNewBalance()
        {
            // Arrange
            int userId = 2;
            var tradeDto = new TradeDto
            {
                PaymentCurrency = "USD",
                PaymentAmount = 50m,
                CurrencyToBuy = "ETH",
                AmountToBuy = 0.1m,
                Name = "Ethereum"
            };

            // Act
            var result = await _tradeService.BuyAsync(userId, tradeDto);

            // Assert
            Assert.IsTrue(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == "USD");
            Assert.AreEqual(50m, balance.Amount);

            var ethBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "ETH");
            Assert.IsNotNull(ethBalance);
            Assert.AreEqual(0.1m, ethBalance.Amount);
        }

        [TestMethod]
        public async Task BuyAsync_WalletNotFound()
        {
            // Arrange
            int userId = 99;
            var tradeDto = new TradeDto
            {
                PaymentCurrency = "USD",
                PaymentAmount = 50m,
                CurrencyToBuy = "BTC",
                AmountToBuy = 0.005m,
                Name = "Bitcoin"
            };

            // Act
            var result = await _tradeService.BuyAsync(userId, tradeDto);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SellAsync_Successful()
        {
            // Arrange
            int userId = 1;
            var tradeDto = new TradeDto
            {
                Id = 1,
                PaymentCurrency = "USD",
                PaymentAmount = 500m,
                CurrencyToBuy = "BTC",
                AmountToBuy = 0.01m,
                Name = "Bitcoin"
            };

            // Act
            var result = await _tradeService.SellAsync(userId, tradeDto);

            // Assert
            Assert.IsTrue(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Trades)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var usdBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "USD");
            Assert.AreEqual(700m, usdBalance.Amount);

            var btcBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "BTC");
            Assert.IsNull(btcBalance);

            var trade = wallet.Trades.FirstOrDefault(t => t.Id == tradeDto.Id);
            Assert.IsNotNull(trade);
            Assert.IsTrue(trade.IsSold);
            Assert.AreEqual(500m, trade.SellPrice);
            Assert.AreEqual("USD", trade.SellCurrency);
        }

        [TestMethod]
        public async Task SellAsync_WalletNotFound()
        {
            // Arrange
            int userId = 99;
            var tradeDto = new TradeDto
            {
                Id = 1,
                PaymentCurrency = "USD",
                PaymentAmount = 500m,
                CurrencyToBuy = "BTC",
                AmountToBuy = 0.01m,
                Name = "Bitcoin"
            };

            // Act
            var result = await _tradeService.SellAsync(userId, tradeDto);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SellAsync_AddsNewBalance()
        {
            // Arrange
            int userId = 2;
            var tradeDto = new TradeDto
            {
                Id = 2,
                PaymentCurrency = "EUR",
                PaymentAmount = 50m,
                CurrencyToBuy = "BTC",
                AmountToBuy = 0.01m,
                Name = "Bitcoin"
            };

            // Act
            var result = await _tradeService.SellAsync(userId, tradeDto);

            // Assert
            Assert.IsTrue(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var eurBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "EUR");
            Assert.IsNotNull(eurBalance);
            Assert.AreEqual(50m, eurBalance.Amount);
        }

        [TestMethod]
        public async Task SellAsync_UpdatesBalanceCorrectly()
        {
            // Arrange
            int userId = 1;
            var tradeDto = new TradeDto
            {
                Id = 1,
                PaymentCurrency = "USD",
                PaymentAmount = 50m,
                CurrencyToBuy = "BTC",
                AmountToBuy = 0.005m,
                Name = "Bitcoin"
            };

            // Act
            var result = await _tradeService.SellAsync(userId, tradeDto);

            // Assert
            Assert.IsTrue(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Trades)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var usdBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "USD");
            Assert.AreEqual(250m, usdBalance.Amount);

            var btcBalance = wallet.Balances.FirstOrDefault(b => b.Currency == "BTC");
            Assert.IsNotNull(btcBalance);
            Assert.AreEqual(0.005m, btcBalance.Amount);

            var trade = wallet.Trades.FirstOrDefault(t => t.Id == tradeDto.Id);
            Assert.IsNotNull(trade);
            Assert.IsTrue(trade.IsSold);
            Assert.AreEqual(50m, trade.SellPrice);
            Assert.AreEqual("USD", trade.SellCurrency);
        }

        [TestMethod]
        public async Task RemoveCurrencyFromWatchlistAsync_RemovesCurrency()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = "BTC";

            // Act
            await _tradeService.RemoveCurrencyFromWatchlistAsync(wallet, currency);

            // Assert
            Assert.IsFalse(wallet.Watchlist.Contains(currency));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RemoveCurrencyFromWatchlistAsync_ThrowsException_WhenWalletIsNull()
        {
            // Arrange
            Wallet wallet = null;
            string currency = "BTC";

            // Act
            await _tradeService.RemoveCurrencyFromWatchlistAsync(wallet, currency);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RemoveCurrencyFromWatchlistAsync_ThrowsException_WhenCurrencyIsNull()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = null;

            // Act
            await _tradeService.RemoveCurrencyFromWatchlistAsync(wallet, currency);
        }

        [TestMethod]
        public async Task RemoveCurrencyFromWatchlistAsync_DoesNothing_WhenCurrencyNotInWatchlist()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = "DOGE";

            // Act
            await _tradeService.RemoveCurrencyFromWatchlistAsync(wallet, currency);

            // Assert
            Assert.AreEqual(3, wallet.Watchlist.Count);
            Assert.IsFalse(wallet.Watchlist.Contains(currency));
        }

        [TestMethod]
        public async Task AddCurrencyToWatchlistAsync_AddsCurrency()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = "DOGE";

            // Act
            await _tradeService.AddCurrencyToWatchlistAsync(wallet, currency);

            // Assert
            Assert.IsTrue(wallet.Watchlist.Contains(currency));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddCurrencyToWatchlistAsync_ThrowsException_WhenWalletIsNull()
        {
            // Arrange
            Wallet wallet = null;
            string currency = "BTC";

            // Act
            await _tradeService.AddCurrencyToWatchlistAsync(wallet, currency);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddCurrencyToWatchlistAsync_ThrowsException_WhenCurrencyIsNull()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = null;

            // Act
            await _tradeService.AddCurrencyToWatchlistAsync(wallet, currency);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddCurrencyToWatchlistAsync_ThrowsException_WhenCurrencyIsEmpty()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = string.Empty;

            // Act
            await _tradeService.AddCurrencyToWatchlistAsync(wallet, currency);
        }

        [TestMethod]
        public async Task AddCurrencyToWatchlistAsync_DoesNothing_WhenCurrencyAlreadyInWatchlist()
        {
            // Arrange
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == 1);
            string currency = "BTC";

            // Act
            await _tradeService.AddCurrencyToWatchlistAsync(wallet, currency);

            // Assert
            Assert.AreEqual(3, wallet.Watchlist.Count);
        }


        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
