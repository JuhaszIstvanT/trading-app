using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Claims;
using TradingApp.API.Models;
using TradingApp.API.Models.DTO;

namespace TradingApp.API.Services
{
    public class TradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public TradeService(ApplicationDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
            _apiUrl = "https://api.coinlore.net/api/";
        }

        public async Task<Wallet> GetWalletForCurrentUserAsync(ClaimsPrincipal user)
        {
            var userId = Convert.ToInt32(user.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _context.Wallets
                .Include(w => w.Balances)
                .Include(w => w.Transactions)
                .Include(w => w.Trades)
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<Trade> GetTradeDetailsAsync(int id)
        {
            var trade = await _context.Trades.FindAsync(id);

            return trade;
        }

        public async Task<bool> SetStopLossOrderPrice(int id, decimal price)
        {
            var trade = await GetTradeDetailsAsync(id);
            if (trade == null)
            {
                return false;
            }

            trade.StopLossOrderPrice = price;
            trade.StopLossOrderActive = true;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<decimal> FetchCurrentPrice(string name)
        {
            try
            {
                var tickersEndpoint = $"{_apiUrl}tickers/";
                var tickersResponse = await _httpClient.GetAsync(tickersEndpoint);

                tickersResponse.EnsureSuccessStatusCode();

                var tickersContent = await tickersResponse.Content.ReadAsStringAsync();
                var tickersData = JsonConvert.DeserializeObject<dynamic>(tickersContent);

                var coin = ((IEnumerable<dynamic>)tickersData.data)
                    .FirstOrDefault(c => c.name.ToString().ToLower() == name.ToLower());

                if (coin == null)
                {
                    throw new Exception($"Cryptocurrency '{name}' not found.");
                }

                string coinId = coin.id;

                var tickerEndpoint = $"{_apiUrl}ticker/?id={coinId}";
                var tickerResponse = await _httpClient.GetAsync(tickerEndpoint);

                tickerResponse.EnsureSuccessStatusCode();

                var tickerContent = await tickerResponse.Content.ReadAsStringAsync();
                var tickerData = JsonConvert.DeserializeObject<dynamic>(tickerContent);

                if (tickerData.Count == 0)
                {
                    throw new Exception($"Price data for '{name}' not found.");
                }

                return Convert.ToDecimal(tickerData[0].price_usd.ToString(), CultureInfo.InvariantCulture);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to fetch current price: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> BuyAsync(int userId, TradeDto tradeDto)
        {
            try
            {
                var wallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wallet == null)
                {
                    return false;
                }

                var balance = wallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.PaymentCurrency);
                if (balance == null || balance.Amount < tradeDto.PaymentAmount)
                {
                    return false;
                }

                balance.Amount -= tradeDto.PaymentAmount;

                var currencyToBuyBalance = wallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.CurrencyToBuy);
                if (currencyToBuyBalance == null)
                {
                    wallet.Balances.Add(new Balance
                    {
                        Currency = tradeDto.CurrencyToBuy,
                        Amount = tradeDto.AmountToBuy
                    });
                }
                else
                {
                    currencyToBuyBalance.Amount += tradeDto.AmountToBuy;
                }

                wallet.Trades.Add(new Trade
                {
                    BuyPrice = tradeDto.PaymentAmount,
                    BuyCurrency = tradeDto.PaymentCurrency,
                    Symbol = tradeDto.CurrencyToBuy,
                    Name = tradeDto.Name,
                    Amount = tradeDto.AmountToBuy,
                    Timestamp = DateTime.UtcNow,
                    IsSold = false,
                    UserId = userId,
                    StopLossOrderActive = false
                });

                await _context.SaveChangesAsync();

                await ReplicateTradeForFollowers(userId, tradeDto);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> SellAsync(int userId, TradeDto tradeDto)
        {
            try
            {
                var wallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .Include(w => w.Trades)
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wallet == null)
                {
                    return false;
                }

                var balance = wallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.PaymentCurrency);
                if (balance == null)
                {
                    wallet.Balances.Add(new Balance
                    {
                        Currency = tradeDto.PaymentCurrency,
                        Amount = tradeDto.PaymentAmount
                    });
                }
                else
                {
                    balance.Amount += tradeDto.PaymentAmount;
                }

                var currencyToSellBalance = wallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.CurrencyToBuy);
                if (currencyToSellBalance != null)
                {
                    currencyToSellBalance.Amount -= tradeDto.AmountToBuy;
                    if (currencyToSellBalance.Amount == 0)
                    {
                        wallet.Balances.Remove(currencyToSellBalance);
                    }
                }

                var trade = wallet.Trades.FirstOrDefault(t => t.Id == tradeDto.Id);
                trade.IsSold = true;
                trade.SellPrice = tradeDto.PaymentAmount;
                trade.SellCurrency = tradeDto.PaymentCurrency;

                await _context.SaveChangesAsync();

                await ReplicateSellTradeForFollowers(userId, tradeDto);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task CheckOrders()
        {
            var tradesWithOrders = await _context.Trades.Where(t => t.StopLossOrderActive && !t.IsSold).ToListAsync();

            foreach (var trade in tradesWithOrders)
            {
                var currentPrice = await FetchCurrentPrice(trade.Name);
                currentPrice *= trade.Amount;
                if (trade.StopLossOrderActive && trade.StopLossOrderPrice > currentPrice)
                {
                    TradeDto tradeDto = new TradeDto
                    {
                        Id = trade.Id,
                        AmountToBuy = trade.Amount,
                        CurrencyToBuy = trade.Symbol,
                        Name = trade.Name,
                        PaymentAmount = currentPrice,
                        PaymentCurrency = trade.BuyCurrency
                    };
                    await SellAsync(trade.UserId, tradeDto);
                }
            }
        }

        public async Task RemoveCurrencyFromWatchlistAsync(Wallet wallet, string currency)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (string.IsNullOrEmpty(currency))
            {
                throw new ArgumentNullException(nameof(currency));
            }

            if (wallet.Watchlist.Contains(currency))
            {
                wallet.Watchlist.Remove(currency);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddCurrencyToWatchlistAsync(Wallet wallet, string currency)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (string.IsNullOrEmpty(currency))
            {
                throw new ArgumentNullException(nameof(currency));
            }

            if (!wallet.Watchlist.Contains(currency))
            {
                wallet.Watchlist.Add(currency);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<TraderDto>> GetTopTraders(int count)
        {
            var wallets = await _context.Wallets
                .Include(w => w.Trades)
                .Include(w => w.User)
                .ToListAsync();

            var topTraders = wallets
                .Select(wallet => new TraderDto
                {
                    UserId = wallet.UserId,
                    UserName = wallet.User.UserName,
                    Profit = CalculateTotalProfit(wallet)
                })
                .OrderByDescending(w => w.Profit)
                .Take(count)
                .ToList();

            return topTraders;
        }

        public async Task<TraderDto> GetTrader(int id)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Trades)
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.UserId == id);

            decimal profit = CalculateTotalProfit(wallet);

            var trader = new TraderDto { Profit = profit, UserName = wallet.User.UserName, UserId = wallet.UserId, Wallet = wallet};

            return trader;

        }

        private decimal CalculateTotalProfit(Wallet wallet)
        {
            return wallet.Trades
                .Where(trade => trade.IsSold)
                .Sum(trade => trade.SellPrice - trade.BuyPrice);
        }

        private async Task ReplicateTradeForFollowers(int userId, TradeDto tradeDto)
        {
            var followers = await _context.Follows.Where(f => f.FollowedTraderId == userId).ToListAsync();
            foreach (var follower in followers)
            {
                var followerWallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .FirstOrDefaultAsync(w => w.UserId == follower.FollowerId);

                if (followerWallet == null)
                {
                    continue;
                }

                var followerBalance = followerWallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.PaymentCurrency);
                if (followerBalance == null || followerBalance.Amount < tradeDto.PaymentAmount)
                {
                    continue;
                }

                followerBalance.Amount -= tradeDto.PaymentAmount;

                var followerCurrencyToBuyBalance = followerWallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.CurrencyToBuy);
                if (followerCurrencyToBuyBalance == null)
                {
                    followerWallet.Balances.Add(new Balance
                    {
                        Currency = tradeDto.CurrencyToBuy,
                        Amount = tradeDto.AmountToBuy
                    });
                }
                else
                {
                    followerCurrencyToBuyBalance.Amount += tradeDto.AmountToBuy;
                }

                followerWallet.Trades.Add(new Trade
                {
                    BuyPrice = tradeDto.PaymentAmount,
                    BuyCurrency = tradeDto.PaymentCurrency,
                    Symbol = tradeDto.CurrencyToBuy,
                    Name = tradeDto.Name,
                    Amount = tradeDto.AmountToBuy,
                    Timestamp = DateTime.UtcNow,
                    IsSold = false,
                    UserId = follower.FollowerId,
                    StopLossOrderActive = false
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task ReplicateSellTradeForFollowers(int userId, TradeDto tradeDto)
        {
            var followers = await _context.Follows.Where(f => f.FollowedTraderId == userId).ToListAsync();
            foreach (var follower in followers)
            {
                var followerWallet = await _context.Wallets
                    .Include(w => w.Balances)
                    .Include(w => w.Trades)
                    .FirstOrDefaultAsync(w => w.UserId == follower.FollowerId);

                if (followerWallet == null)
                {
                    continue;
                }

                var followerTrade = followerWallet.Trades.FirstOrDefault(t => t.Symbol == tradeDto.CurrencyToBuy && !t.IsSold && t.Amount == tradeDto.AmountToBuy);
                if (followerTrade == null)
                {
                    continue;
                }

                var followerCurrencyToSellBalance = followerWallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.CurrencyToBuy);
                if (followerCurrencyToSellBalance != null)
                {
                    followerCurrencyToSellBalance.Amount -= tradeDto.AmountToBuy;
                    if (followerCurrencyToSellBalance.Amount == 0)
                    {
                        followerWallet.Balances.Remove(followerCurrencyToSellBalance);
                    }
                }

                var followerBalance = followerWallet.Balances.FirstOrDefault(b => b.Currency == tradeDto.PaymentCurrency);
                if (followerBalance == null)
                {
                    followerWallet.Balances.Add(new Balance
                    {
                        Currency = tradeDto.PaymentCurrency,
                        Amount = tradeDto.PaymentAmount
                    });
                }
                else
                {
                    followerBalance.Amount += tradeDto.PaymentAmount;
                }

                followerTrade.IsSold = true;
                followerTrade.SellPrice = tradeDto.PaymentAmount;
                followerTrade.SellCurrency = tradeDto.PaymentCurrency;

                await _context.SaveChangesAsync();
            }
        }

    }
}
