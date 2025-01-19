using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using TradingApp.API.Models;
using Transaction = TradingApp.API.Models.Transaction;

namespace TradingApp.API.Services
{
    public class WalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsValidTransfer(string userID, string from, string to, string currency, decimal amount)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(userID));

            if (wallet == null || wallet.Address == to || wallet.Address != from || from == to)
            {
                return false;
            }

            var recipientWallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.Address == to);


            if (recipientWallet == null)
            {
                return false;
            }

            Balance senderBalance = wallet.Balances.FirstOrDefault(b => b.Currency == currency);

            if (senderBalance == null)
            {
                return false;
            }

            if (senderBalance.Amount < amount)
            {
                return false;
            }
            return true;
        }

        public async Task<Transaction> AddTransaction(Transaction transaction)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == transaction.UserId);

            if (wallet == null)
            {
                throw new Exception("Sender wallet not found");
            }

            var recipientWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.Address == transaction.To);

            if (recipientWallet == null)
            {
                throw new Exception("Recipient wallet not found");
            }

            var newTransaction = new Transaction
            {
                Date = transaction.Date,
                Type = transaction.Type,
                Currency = transaction.Currency,
                Amount = transaction.Amount,
                From = transaction.From,
                To = transaction.To,
                TransactionFee = transaction.TransactionFee,
                IsPending = transaction.IsPending,
                UserId = transaction.UserId
            };

            wallet.Transactions.Add(newTransaction);

            var recipientTransaction = new Transaction
            {
                Date = transaction.Date,
                Type = transaction.Type,
                Currency = transaction.Currency,
                Amount = transaction.Amount,
                From = transaction.From,
                To = transaction.To,
                TransactionFee = transaction.TransactionFee,
                IsPending = transaction.IsPending,
                UserId = recipientWallet.UserId
            };

            recipientWallet.Transactions.Add(recipientTransaction);

            await _context.SaveChangesAsync();

            return newTransaction;
        }

        public async Task TransferFundsAsync(List<Transaction> transactions)
        {
            foreach (Transaction transaction in transactions)
            {
                var wallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .Include(w => w.Transactions)
                    .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(transaction.UserId));

                if (wallet == null)
                {
                    throw new Exception("Sender wallet not found");
                }

                var recipientWallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .FirstOrDefaultAsync(w => w.Address == transaction.To);

                if (recipientWallet == null)
                {
                    throw new Exception("Recipient wallet not found");
                }

                Balance senderBalance = wallet.Balances.FirstOrDefault(b => b.Currency == transaction.Currency);

                if (senderBalance.Amount < transaction.Amount)
                {
                    throw new Exception("Balance is not sufficient");
                }

                senderBalance.Amount -= transaction.Amount;

                Balance receiverBalance = recipientWallet.Balances.FirstOrDefault(b => b.Currency == transaction.Currency);

                if (receiverBalance == null)
                {
                    receiverBalance = new Balance { Currency = transaction.Currency, Amount = transaction.Amount };
                    recipientWallet.Balances.Add(receiverBalance);
                }
                else
                {
                    receiverBalance.Amount += transaction.Amount;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task DepositFundsAsync(string userId, string currency, decimal amount)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(userId));

            if (wallet == null)
            {
                throw new Exception("User wallet not found");
            }

            var balance = wallet.Balances.FirstOrDefault(b => b.Currency == currency);

            if (balance == null)
            {
                wallet.Balances.Add(new Balance { Currency = currency, Amount = amount });
            }
            else
            {
                balance.Amount += amount;
            }

            var newTransaction = new Transaction
            {
                Date = DateTime.Now,
                Type = "Deposit",
                Currency = currency,
                Amount = amount,
                From = "own",
                To = "own",
                TransactionFee = 0,
                IsPending = false,
                UserId = Convert.ToInt32(userId)
            };

            wallet.Transactions.Add(newTransaction);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> WithdrawFundsAsync(string userId, string currency, decimal amount)
        {
            try
            {
                var wallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(userId));

                if (wallet == null)
                {
                    throw new Exception("User wallet not found");
                }

                var balance = wallet.Balances.FirstOrDefault(b => b.Currency == currency);

                if (balance == null || balance.Amount < amount)
                {
                    throw new Exception("Balance is not sufficient");
                }

                balance.Amount -= amount;

                var newTransaction = new Transaction
                {
                    Date = DateTime.Now,
                    Type = "Withdrawal",
                    Currency = currency,
                    Amount = amount,
                    From = "own",
                    To = "own",
                    TransactionFee = 0,
                    IsPending = false,
                    UserId = Convert.ToInt32(userId)
                };

                wallet.Transactions.Add(newTransaction);

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Wallet> GetWalletForCurrentUserAsync(ClaimsPrincipal user)
        {
            var userId = Convert.ToInt32(user.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<bool> CreateWalletForUserAsync(int userId)
        {
            try
            {
                byte[] privateKey;
                using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
                {
                    privateKey = ecdsa.ExportECPrivateKey();
                }

                byte[] publicKey;
                using (ECDsa ecdsa = ECDsa.Create())
                {
                    ecdsa.ImportECPrivateKey(privateKey, out _);
                    publicKey = ecdsa.ExportSubjectPublicKeyInfo();
                }

                string address = ComputeAddress(publicKey);

                var wallet = new Wallet { UserId = userId };

                wallet.PrivateKey = BitConverter.ToString(privateKey).Replace("-", "").ToLower();
                wallet.PublicKey = BitConverter.ToString(publicKey).Replace("-", "").ToLower();

                wallet.Address = address;

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        private string ComputeAddress(byte[] publicKey)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(publicKey);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
