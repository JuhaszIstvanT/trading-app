using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingApp.API.Models;
using TradingApp.API.Services;

namespace TradingApp.Tests
{
    [TestClass]
    public class WalletServiceTest
    {
        private WalletService _walletService;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _walletService = new WalletService(_context);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var wallets = new List<Wallet>
            {
                new Wallet
                {
                    UserId = 1,
                    Address = "fromAddress",
                    PrivateKey = "privateKey1",
                    PublicKey = "publicKey1",
                    Balances = new List<Balance>
                    {
                        new Balance { Currency = "USD", Amount = 200m }
                    }
                },
                new Wallet
                {
                    UserId = 2,
                    Address = "toAddress",
                    PrivateKey = "privateKey2",
                    PublicKey = "publicKey2",
                    Balances = new List<Balance>
                    {
                        new Balance { Currency = "USD", Amount = 0m }
                    }
                },
                new Wallet
                {
                    UserId = 3,
                    Address = "newAddress",
                    PrivateKey = "privateKey3",
                    PublicKey = "publicKey3",
                    Balances = new List<Balance>(),
                    Transactions = new List<Transaction>()
                }
            };

            _context.Wallets.AddRange(wallets);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task IsValidTransfer_ReturnsFalse_WhenWalletNotFound()
        {
            // Arrange
            string userId = "99";
            string from = "fromAddress";
            string to = "toAddress";
            string currency = "USD";
            decimal amount = 100m;

            // Act
            var result = await _walletService.IsValidTransfer(userId, from, to, currency, amount);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsValidTransfer_ReturnsFalse_WhenSenderAddressIsRecipientAddress()
        {
            // Arrange
            string userId = "1";
            string from = "sameAddress";
            string to = "sameAddress";
            string currency = "USD";
            decimal amount = 100m;

            // Act
            var result = await _walletService.IsValidTransfer(userId, from, to, currency, amount);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsValidTransfer_ReturnsFalse_WhenRecipientWalletNotFound()
        {
            // Arrange
            string userId = "1";
            string from = "fromAddress";
            string to = "nonExistentAddress";
            string currency = "USD";
            decimal amount = 100m;

            // Act
            var result = await _walletService.IsValidTransfer(userId, from, to, currency, amount);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsValidTransfer_ReturnsFalse_WhenSenderBalanceIsInsufficient()
        {
            // Arrange
            string userId = "1";
            string from = "fromAddress";
            string to = "toAddress";
            string currency = "USD";
            decimal amount = 300m;

            // Act
            var result = await _walletService.IsValidTransfer(userId, from, to, currency, amount);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsValidTransfer_ReturnsTrue_WhenValid()
        {
            // Arrange
            string userId = "1";
            string from = "fromAddress";
            string to = "toAddress";
            string currency = "USD";
            decimal amount = 100m;

            // Act
            var result = await _walletService.IsValidTransfer(userId, from, to, currency, amount);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task DepositFundsAsync_AddsFundsToExistingBalance()
        {
            // Arrange
            string userId = "1";
            string currency = "USD";
            decimal amount = 100m;

            // Act
            await _walletService.DepositFundsAsync(userId, currency, amount);
            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(userId));

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == currency);

            // Assert
            Assert.IsNotNull(balance);
            Assert.AreEqual(300m, balance.Amount);
            Assert.AreEqual(1, wallet.Transactions.Count);
            Assert.AreEqual("Deposit", wallet.Transactions.First().Type);
            Assert.AreEqual(amount, wallet.Transactions.First().Amount);
        }

        [TestMethod]
        public async Task DepositFundsAsync_AddsFundsToNewBalance()
        {
            // Arrange
            string userId = "2";
            string currency = "EUR";
            decimal amount = 100m;

            // Act
            await _walletService.DepositFundsAsync(userId, currency, amount);
            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(userId));

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == currency);

            // Assert
            Assert.IsNotNull(balance);
            Assert.AreEqual(100, balance.Amount);
            Assert.AreEqual(amount, balance.Amount);
            Assert.AreEqual(1, wallet.Transactions.Count);
            Assert.AreEqual("Deposit", wallet.Transactions.First().Type);
            Assert.AreEqual(amount, wallet.Transactions.First().Amount);
        }

        [TestMethod]
        public async Task DepositFundsAsync_ThrowsException_WhenWalletNotFound()
        {
            // Arrange
            string userId = "99";
            string currency = "USD";
            decimal amount = 100m;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _walletService.DepositFundsAsync(userId, currency, amount);
            });
        }

        [TestMethod]
        public async Task WithdrawFundsAsync_SuccessfulWithdrawal()
        {
            // Arrange
            string userId = "1";
            string currency = "USD";
            decimal withdrawalAmount = 100m;

            // Act
            var result = await _walletService.WithdrawFundsAsync(userId, currency, withdrawalAmount);

            // Assert
            Assert.IsTrue(result);

            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(userId));

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == currency);
            Assert.IsNotNull(balance);
            Assert.AreEqual(100, balance.Amount);
        }

        [TestMethod]
        public async Task WithdrawFundsAsync_WalletNotFound()
        {
            // Arrange
            string userId = "99";
            string currency = "USD";
            decimal withdrawalAmount = 100m;

            // Act
            var result = await _walletService.WithdrawFundsAsync(userId, currency, withdrawalAmount);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task WithdrawFundsAsync_InsufficientBalance()
        {
            // Arrange
            string userId = "1";
            string currency = "USD";
            decimal withdrawalAmount = 300m;

            // Act
            var result = await _walletService.WithdrawFundsAsync(userId, currency, withdrawalAmount);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AddTransaction_Successful()
        {
            // Arrange
            var transaction = new Transaction
            {
                UserId = 1,
                To = "toAddress",
                Currency = "USD",
                Type = "transfer",
                From = "fromAddress"
            };

            // Act
            var result = await _walletService.AddTransaction(transaction);

            // Assert
            Assert.IsNotNull(result);

            var senderWallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == transaction.UserId);

            Assert.IsNotNull(senderWallet);
            Assert.IsTrue(senderWallet.Transactions.Any(t => t.Id == result.Id));

            var recipientWallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Address == transaction.To);

            Assert.IsNotNull(recipientWallet);
        }

        [TestMethod]
        public async Task AddTransaction_SenderWalletNotFound()
        {
            // Arrange
            var transaction = new Transaction
            {
                UserId = 99,
                Currency = "USD",
                Type = "SomeType",
                From = "SomeFrom",
                To = "toAddress",
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _walletService.AddTransaction(transaction);
            }, "Sender wallet not found");
        }

        [TestMethod]
        public async Task AddTransaction_RecipientWalletNotFound()
        {
            // Arrange
            var transaction = new Transaction
            {
                UserId = 1,
                To = "nonExistentAddress",
                Currency = "USD",
                Type = "transfer",
                From = "fromAddress",
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _walletService.AddTransaction(transaction);
            }, "Recipient wallet not found");
        }

        [TestMethod]
        public async Task TransferFundsAsync_Successful()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    UserId = 1,
                    To = "toAddress",
                    Currency = "USD",
                    Amount = 100m,
                    Date = DateTime.Now,
                    Type = "Transfer",
                    From = "fromAddress",
                    TransactionFee = 0,
                    IsPending = false
                }
            };

            // Act
            await _walletService.TransferFundsAsync(transactions);

            // Assert
            var senderWallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == 1);
            var recipientWallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.Address == "toAddress");

            Assert.AreEqual(100m, senderWallet.Balances.First(b => b.Currency == "USD").Amount);
            Assert.AreEqual(100m, recipientWallet.Balances.First(b => b.Currency == "USD").Amount);
        }

        [TestMethod]
        public async Task TransferFundsAsync_SenderWalletNotFound()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    UserId = 99,
                    To = "toAddress",
                    Currency = "USD",
                    Amount = 100m,
                    Date = DateTime.Now,
                    Type = "Transfer",
                    From = "fromAddress",
                    TransactionFee = 0,
                    IsPending = false
                }
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _walletService.TransferFundsAsync(transactions);
            }, "Sender wallet not found");
        }

        [TestMethod]
        public async Task TransferFundsAsync_RecipientWalletNotFound()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    UserId = 1,
                    To = "nonExistentAddress",
                    Currency = "USD",
                    Amount = 100m,
                    Date = DateTime.Now,
                    Type = "Transfer",
                    From = "fromAddress",
                    TransactionFee = 0,
                    IsPending = false
                }
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _walletService.TransferFundsAsync(transactions);
            }, "Recipient wallet not found");
        }

        [TestMethod]
        public async Task TransferFundsAsync_InsufficientFunds()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    UserId = 1,
                    To = "toAddress",
                    Currency = "USD",
                    Amount = 300m,
                    Date = DateTime.Now,
                    Type = "Transfer",
                    From = "fromAddress",
                    TransactionFee = 0,
                    IsPending = false
                }
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _walletService.TransferFundsAsync(transactions);
            }, "Balance is not sufficient");
        }

        [TestMethod]
        public async Task TransferFundsAsync_RecipientBalanceAdded()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    UserId = 1,
                    To = "newAddress",
                    Currency = "USD",
                    Amount = 100m,
                    Date = DateTime.Now,
                    Type = "Transfer",
                    From = "fromAddress",
                    TransactionFee = 0,
                    IsPending = false
                }
            };

            // Act
            await _walletService.TransferFundsAsync(transactions);

            // Assert
            var senderWallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == 1);
            var recipientWallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.Address == "newAddress");

            Assert.AreEqual(100m, senderWallet.Balances.First(b => b.Currency == "USD").Amount);
            var recipientBalance = recipientWallet.Balances.FirstOrDefault(b => b.Currency == "USD");
            Assert.IsNotNull(recipientBalance);
            Assert.AreEqual(100m, recipientBalance.Amount);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
