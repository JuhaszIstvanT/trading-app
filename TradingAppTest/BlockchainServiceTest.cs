using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TradingApp.API.Models;
using TradingApp.API.Services;

namespace TradingApp.Tests
{
    [TestClass]
    public class BlockchainServiceTests
    {
        private BlockchainService _blockchainService;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _blockchainService = new BlockchainService(_context);

            SeedDatabase();
        }

        private void SeedDatabase()
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

            _context.Blocks.AddRange(blocks);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetLatestBlock_ReturnsLatestBlock()
        {
            var latestBlock = await _blockchainService.GetLatestBlock();

            Assert.IsNotNull(latestBlock);
            Assert.AreEqual(2, latestBlock.Id);
        }




        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
