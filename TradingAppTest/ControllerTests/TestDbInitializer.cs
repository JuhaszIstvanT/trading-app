using System;
using System.Collections.Generic;
using TradingApp.API.Models;

namespace TradingAppTest.ControllerTests
{
    public static class TestDbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            var blocks = new List<Block>
            {
                new Block
                {
                    Id = 1,
                    TimeStamp = DateTime.UtcNow,
                    PreviousHash = "0",
                    Height = 1,
                    Nonce = 123,
                    Transactions = new List<Transaction>()
                },
                new Block
                {
                    Id = 2,
                    TimeStamp = DateTime.UtcNow,
                    PreviousHash = "1",
                    Height = 2,
                    Nonce = 456,
                    Transactions = new List<Transaction>()
                }
            };

            context.Blocks.AddRange(blocks);

            var wallets = new List<Wallet>
            {
                new Wallet
                {
                    UserId = 1,
                    PrivateKey = "samplePrivateKey",
                    PublicKey = "samplePublicKey",
                    Address = "sampleAddress",
                    Balances = new List<Balance>
                    {
                        new Balance { Currency = "BTC", Amount = 0.5m },
                        new Balance { Currency = "ETH", Amount = 10m },
                        new Balance {Currency = "USD", Amount = 500}
                    },
                    Transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            Id = 1,
                            Date = DateTime.UtcNow,
                            Type = "Deposit",
                            Currency = "BTC",
                            Amount = 0.1m,
                            TransactionFee = 0.0001m,
                            From = "address1",
                            To = "sampleAddress",
                            IsPending = true,
                            IsValid = false,
                            UserId = 1
                        },
                        new Transaction
                        {
                            Id = 2,
                            Date = DateTime.UtcNow,
                            Type = "Withdrawal",
                            Currency = "ETH",
                            Amount = 1.0m,
                            TransactionFee = 0.01m,
                            From = "sampleAddress",
                            To = "address2",
                            IsPending = false,
                            IsValid = false,
                            UserId = 1
                        }
                    },
                    Watchlist = new List<string> { "BTC", "ETH" },
                    Trades = new List<Trade>
                    {
                        new Trade
                        {
                            BuyPrice = 40000,
                            BuyCurrency = "USD",
                            SellPrice = 0,
                            SellCurrency = "",
                            Symbol = "BTC",
                            Name = "Bitcoin",
                            Amount = 1,
                            IsSold = false,
                            Timestamp = DateTime.UtcNow,
                            StopLossOrderPrice = 0,
                            StopLossOrderActive = false,
                            UserId = 1
                        },
                        new Trade
                        {
                            BuyPrice = 2500,
                            BuyCurrency = "USD",
                            SellPrice = 0,
                            SellCurrency = "",
                            Symbol = "ETH",
                            Name = "Ethereum",
                            Amount = 10,
                            IsSold = false,
                            Timestamp = DateTime.UtcNow,
                            StopLossOrderPrice = 0,
                            StopLossOrderActive = false,
                            UserId = 1
                        },
                        new Trade
                        {
                            BuyPrice = 2500,
                            BuyCurrency = "USD",
                            SellPrice = 3000,
                            SellCurrency = "USD",
                            Symbol = "ETH",
                            Name = "Ethereum",
                            Amount = 10,
                            IsSold = true,
                            Timestamp = DateTime.UtcNow,
                            StopLossOrderPrice = 0,
                            StopLossOrderActive = false,
                            UserId = 1
                        }
                    }
                }
            };

            context.Wallets.AddRange(wallets);

            var follows = new List<Follow>
            {
                new Follow { FollowerId = 1, FollowedTraderId = 2 },
                new Follow { FollowerId = 2, FollowedTraderId = 3 },
                new Follow { FollowerId = 3, FollowedTraderId = 1 }
            };

            context.Follows.AddRange(follows);

            context.SaveChanges();
        }
    }
}
