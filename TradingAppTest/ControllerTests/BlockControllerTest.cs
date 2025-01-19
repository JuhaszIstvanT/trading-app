using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingApp.API.Controllers;
using TradingApp.API.Models;
using TradingApp.API.Services;

namespace TradingAppTest.ControllerTests
{
    [TestClass]
    public class BlockControllerTest
    {
        private ApplicationDbContext _context;
        private BlockchainService _service;
        private BlockController _controller;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            TestDbInitializer.Initialize(_context);

            _service = new BlockchainService(_context);
            _controller = new BlockController(_service);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task GetBlocks_ReturnsListOfBlocks()
        {
            var result = await _controller.GetBlocks();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);

            var blocks = okResult.Value as IEnumerable<Block>;
            Assert.IsNotNull(blocks);
            Assert.AreEqual(2, blocks.Count());
        }

        [TestMethod]
        public async Task GetBlockById_ReturnsBlockWithGivenId()
        {
            // Arrange
            var id = 1;

            // Act
            var result = await _controller.GetBlockById(id);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);

            var block = okResult.Value as Block;
            Assert.IsNotNull(block);
            Assert.AreEqual(id, block.Id);
        }

        [TestMethod]
        public async Task GetBlockById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var id = 99;

            // Act
            var result = await _controller.GetBlockById(id);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
