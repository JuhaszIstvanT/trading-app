using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using TradingApp.API.Models;
using TradingApp.API.Models.DTO;
using TradingApp.API.Services;

namespace TradingApp.API.BackgroundServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TcpListener server = null;
            try
            {
                server = new TcpListener(IPAddress.Any, 12000);
                server.Start();

                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    await HandleClientAsync(client);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                server?.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();

                StringBuilder builder = new StringBuilder();
                byte[] buffer = new byte[1024];

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    builder.Append(data);
                }

                string receivedData = builder.ToString();

                string[] parts = receivedData.Split('|');
                char dataType = parts[0][0];

                try
                {
                    if (dataType == 'B')
                    {
                        int port = int.Parse(parts[1]);
                        string blockJson = parts[2];
                        Block block = JsonConvert.DeserializeObject<Block>(blockJson);

                        await HandleBlockAsync(block, port);
                    }
                    else if (dataType == 'T')
                    {
                        await HandleTransactionAsync(parts);
                    }
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private async Task HandleBlockAsync(Block block, int port)
        {
            int difficulty = 3;

            Thread miningThread = new Thread(async () =>
            {
                int solution = Mine(block, difficulty);
                string prevHash = CalculatePreviousBlockHash(block);
                Block newBlock = new Block { Height = block.Height + 1, Nonce = solution, TimeStamp = DateTime.Now, PreviousHash = prevHash };
                BlockMinerPort bmp = new BlockMinerPort { Block = newBlock, Port = port };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var blockchainService = scope.ServiceProvider.GetRequiredService<BlockchainService>();
                    var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();

                    var pendingTransactions = await blockchainService.AddBlock(bmp.Block, bmp.Port);

                    if (pendingTransactions != null)
                    {
                        try
                        {
                            await walletService.TransferFundsAsync(pendingTransactions);
                            _logger.LogInformation("Transfer successful.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("An error occurred during transfer: {error}", ex.Message);
                        }
                    }
                    else
                    {
                        _logger.LogError("An error occurred while adding transactions to the block.");
                    }
                }

                _logger.LogInformation("Solution found by port {port}: {solution}", port, solution);
            });
            miningThread.Start();

            _logger.LogInformation("Received block from port {port}: {block}", port, block.Height);
        }

        private async Task HandleTransactionAsync(string[] parts)
        {
            string transactionData = parts[1];
            byte[] txData = Encoding.UTF8.GetBytes(transactionData);
            string signature = parts[2];
            byte[] signatureBytes = Convert.FromBase64String(signature);
            string publicKey = parts[3];
            int transactionId = Convert.ToInt32(parts[4]);

            bool isValid;
            using (ECDsa ecdsa = ECDsa.Create())
            {
                ecdsa.ImportSubjectPublicKeyInfo(HexStringToByteArray(publicKey), out _);
                isValid = ecdsa.VerifyData(txData, signatureBytes, HashAlgorithmName.SHA256);
            }

            if (isValid)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var blockchainService = scope.ServiceProvider.GetRequiredService<BlockchainService>();

                    try
                    {
                        await blockchainService.ValidateTransaction(transactionId);
                        _logger.LogInformation("Validation successful.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("An error occurred during validation: {error}", ex.Message);
                    }
                }
            }
        }

        private string CalculateHash(long nonce, string prevHash)
        {
            string hash = $"{prevHash}{nonce}";
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hash));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private string CalculatePreviousBlockHash(Block block)
        {
            string hash = $"{block.PreviousHash}{block.Nonce}";
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hash));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private int Mine(Block block, int difficulty)
        {
            string prevHash = CalculatePreviousBlockHash(block);
            int solution = 1;
            string targetPrefix = new string('0', difficulty);

            while (true)
            {
                string hash = CalculateHash(solution, prevHash);

                if (hash.StartsWith(targetPrefix))
                {
                    return solution;
                }

                solution++;
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}
