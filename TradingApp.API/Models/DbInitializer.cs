using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace TradingApp.API.Models
{
    public class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                if (!context.Users.Any())
                {
                    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
                    var names = new List<string>
                    {
                        "Alice", "Bob", "Charlie", "Dave", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy",
                        "Mallory", "Niaj", "Olivia", "Peggy", "Quentin", "Rupert", "Sybil", "Trent", "Uma", "Victor",
                        "Wendy", "Xavier", "Yvonne", "Zach", "Angela", "Brian", "Carla", "Daniel", "Elena",
                        "Freddie", "Gina", "Harry", "Isabel", "Jack", "Karen", "Leo", "Mona", "Nate", "Opal",
                        "Peter", "Quincy", "Rita", "Sam", "Tina", "Umar", "Violet", "Wade", "Xena", "Yara", "Zane",
                        "Adrian", "Blake", "Cathy", "Derek", "Erin", "Felix", "Gloria", "Hank", "Irene", "Jon",
                        "Kyle", "Laura", "Mason", "Nina", "Oscar", "Paula", "Quinn", "Rachel", "Sean", "Tara",
                        "Ulysses", "Vera", "Will", "Xander", "Yolanda", "Zeke", "Anna", "Ben", "Clara", "Dylan",
                        "Ella", "Finn", "Holly", "James", "Katie", "Liam", "Mia", "Noah", "Owen", "Piper",
                        "Reed", "Sara", "Tom", "Vince", "Wes", "Ximena", "Yuri", "Zara"
                    };

                    var random = new Random();
                    names = names.OrderBy(x => random.Next()).ToList();

                    var usersData = new List<(string username, string email, string password)>();
                    foreach (var name in names)
                    {
                        string username = name.ToLower();
                        string email = $"{username}@example.com";
                        string password = $"Password{username}";
                        usersData.Add((username, email, password));
                    }

                    foreach (var (username, email, password) in usersData)
                    {
                        string debitCardNumber = GenerateValidCardNumber();
                        var user = CreateUser(userManager, username, email, password, debitCardNumber);
                        context.SaveChanges();
                        var wallet = CreateWallet(context, user);
                        AddTrades(context, wallet, GenerateTrades(random));
                        AddWatchlist(wallet, "btc", "eth");
                    }

                    var genesisBlock = new Block { TimeStamp = DateTime.Now, Nonce = 0, Height = 0, PreviousHash = "" };
                    context.Blocks.Add(genesisBlock);

                    var miner = new Miner { IPAddress = "127.0.0.1", Port = 12345, Balance = 0 };
                    context.Miners.Add(miner);

                    context.SaveChanges();
                }
            }
        }

        private static User CreateUser(UserManager<User> userManager, string username, string email, string password, string debitCardNumber)
        {
            var user = new User { UserName = username, Email = email, DebitCardNumber = debitCardNumber };
            var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            return user;
        }

        private static Wallet CreateWallet(ApplicationDbContext context, User user)
        {
            byte[] privateKey;
            using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                privateKey = ecdsa.ExportECPrivateKey();
            }
            string privateKeyStr = BitConverter.ToString(privateKey).Replace("-", "").ToLower();

            byte[] publicKey;
            using (ECDsa ecdsa = ECDsa.Create())
            {
                ecdsa.ImportECPrivateKey(privateKey, out _);
                publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            }
            string publicKeyStr = BitConverter.ToString(publicKey).Replace("-", "").ToLower();

            string address = ComputeAddress(publicKey);

            var wallet = new Wallet
            {
                UserId = user.Id,
                PrivateKey = privateKeyStr,
                PublicKey = publicKeyStr,
                Address = address
            };
            context.Wallets.Add(wallet);
            return wallet;
        }

        private static List<Trade> GenerateTrades(Random random)
        {
            var trades = new List<Trade>();
            int numTrades = random.Next(10, 50);
            for (int i = 0; i < numTrades; i++)
            {
                decimal buyPrice = random.Next(1000, 50000);
                decimal sellPrice = random.Next(1000, 50000);
                trades.Add(new Trade
                {
                    Timestamp = DateTime.Now.AddDays(-random.Next(1, 100)),
                    Symbol = random.Next(0, 2) == 0 ? "BTC" : "ETH",
                    Amount = random.Next(1, 10),
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    SellCurrency = "USD",
                    BuyCurrency = "USD",
                    IsSold = true,
                    Name = random.Next(0, 2) == 0 ? "bitcoin" : "ethereum",
                    UserId = 0
                });
            }
            return trades;
        }

        private static void AddTrades(ApplicationDbContext context, Wallet wallet, List<Trade> trades)
        {
            foreach (var trade in trades)
            {
                trade.UserId = wallet.UserId;
                context.Trades.Add(trade);
            }
            wallet.Trades.AddRange(trades);
        }

        private static void AddWatchlist(Wallet wallet, params string[] symbols)
        {
            wallet.Watchlist.AddRange(symbols);
        }

        private static string ComputeAddress(byte[] publicKey)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(publicKey);
                return Convert.ToBase64String(hash);
            }
        }

        private static string GenerateValidCardNumber()
        {
            var random = new Random();
            var cardNumber = new StringBuilder();
            for (int i = 0; i < 15; i++)
            {
                cardNumber.Append(random.Next(0, 10));
            }
            int sum = 0;
            bool doubleDigit = true;
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(cardNumber[i].ToString());
                if (doubleDigit)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
                doubleDigit = !doubleDigit;
            }
            int checkDigit = (sum * 9) % 10;
            cardNumber.Append(checkDigit);
            return cardNumber.ToString();
        }

    }
}
