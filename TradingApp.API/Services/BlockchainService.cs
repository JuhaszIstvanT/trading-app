using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TradingApp.API.Models;

namespace TradingApp.API.Services
{
    public class BlockchainService
    {
        private readonly ApplicationDbContext _context;

        public BlockchainService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Block>> GetBlocksAsync()
        {
            return await _context.Blocks.Include(b => b.Transactions).ToListAsync();
        }

        public async Task<Block> GetBlockByIdAsync(int id)
        {
            return await _context.Blocks
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Block> GetLatestBlock()
        {
            return await _context.Blocks.OrderByDescending(b => b.Height).FirstOrDefaultAsync();
        }

        public async Task<List<Transaction>> AddBlock(Block block, int port)
        {
            Block latestBlock = await _context.Blocks.OrderByDescending(b => b.Height).FirstOrDefaultAsync();

            string latestBlockHash = CalculateHash(latestBlock);

            if (latestBlock.Height < block.Height && latestBlockHash == block.PreviousHash)
            {
                Miner miner = await _context.Miners.FirstOrDefaultAsync(m => m.Port == port);

                var transactions = await _context.Transactions
                    .Where(t => t.IsPending)
                    .ToListAsync();

                foreach (var transaction in transactions)
                {
                    transaction.IsPending = false;
                }

                decimal totalTransactionFees = transactions.Sum(t => t.TransactionFee);

                if (miner != null)
                {
                    miner.Balance += totalTransactionFees;
                }

                block.Transactions.AddRange(transactions);

                await _context.Blocks.AddAsync(block);

                await _context.SaveChangesAsync();

                return transactions;
            }
            return null;
        }

        public async Task ValidateTransaction(int transactionId)
        {
            Transaction transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId);
            transaction.IsValid = true;

            await _context.SaveChangesAsync();
        }

        public async Task SendBlockToMiners()
        {
            var transactions = await _context.Transactions.ToListAsync();
            int distinctPendingTransactions = transactions.Where(t => t.IsPending).Count();

            if (distinctPendingTransactions > 2)
            {
                Block block = await GetLatestBlock();

                var miners = await _context.Miners.ToListAsync();

                foreach (var miner in miners)
                {
                    int port = miner.Port;


                    using (TcpClient client = new TcpClient("127.0.0.1", 12000))
                    {
                        NetworkStream stream = client.GetStream();

                        string blockJson = JsonConvert.SerializeObject(block);

                        string data = $"B|{port}|{blockJson}";

                        byte[] bytes = Encoding.ASCII.GetBytes(data);

                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        public async Task SendTransactionToMiners(Transaction transaction)
        {
            Wallet wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == Convert.ToInt32(transaction.UserId));

            string publicKey = wallet.PublicKey;

            using (TcpClient client = new TcpClient("127.0.0.1", 12000))
            {
                NetworkStream stream = client.GetStream();

                string transactionData = SerializeTransaction(transaction);

                byte[] signature = await SignTransaction(transaction);

                string signatureStr = Convert.ToBase64String(signature);

                int transactionId = transaction.Id;

                string data = $"T|{transactionData}|{signatureStr}|{publicKey}|{transactionId}";

                byte[] bytes = Encoding.ASCII.GetBytes(data);

                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        private async Task<byte[]> SignTransaction(Transaction transaction)
        {
            Wallet wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == transaction.UserId);

            string privateKey = wallet.PrivateKey;

            string transactionData = SerializeTransaction(transaction);

            byte[] txData = Encoding.UTF8.GetBytes(transactionData);

            using (ECDsa ecdsa = ECDsa.Create())
            {
                ecdsa.ImportECPrivateKey(HexStringToByteArray(privateKey), out _);
                byte[] signature = ecdsa.SignData(txData, HashAlgorithmName.SHA256);
                return signature;
            }
        }

        private string CalculateHash(Block block)
        {
            string hash = $"{block.PreviousHash}{block.Nonce}";
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hash));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private string SerializeTransaction(Transaction transaction)
        {
            return $"{transaction.From}{transaction.To}{transaction.Amount}{transaction.Currency}";
        }
    }
}
