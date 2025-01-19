using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TradingApp.API.Controllers;
using TradingApp.API.Models.DTO;
using TradingApp.API.Models;
using TradingApp.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TradingAppTest.ControllerTests
{
    [TestClass]
    public class FollowControllerTest
    {
        private FollowService _tradeService;
        private FollowController _controller;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            TestDbInitializer.Initialize(_context);

            _tradeService = new FollowService(_context);

            _controller = new FollowController(_tradeService);
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Name, "testuser")
                    }, "mock"))
                }
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task FollowTrader_ReturnsOkResultAndIncreasedFollowerCount()
        {
            // Arrange
            var followerId = 1;
            var followedTraderId = 3;

            var initialFollowedTraders = await GetFollowedTradersCount(followerId);

            // Act
            var result = await _controller.FollowTrader(new FollowRequestDto { FollowerId = followerId, FollowedTraderId = followedTraderId });

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var updatedFollowedTraders = await GetFollowedTradersCount(followerId);

            Assert.IsTrue(updatedFollowedTraders > initialFollowedTraders);
        }

        [TestMethod]
        public async Task UnfollowTrader_ReturnsOkResultAndDecreasedFollowerCount()
        {
            // Arrange
            var followerId = 1;
            var followedTraderId = 2;

            var initialFollowedTraders = await GetFollowedTradersCount(followerId);

            // Act
            var result = await _controller.UnfollowTrader(new FollowRequestDto { FollowerId = followerId, FollowedTraderId = followedTraderId });

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var updatedFollowedTraders = await GetFollowedTradersCount(followerId);

            Assert.IsTrue(updatedFollowedTraders < initialFollowedTraders);
        }

        [TestMethod]
        public async Task IsFollowing_ReturnsOkResult()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId = 2;

            // Act
            var result = await _controller.IsFollowing(followerId, followedTraderId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var isFollowing = (bool)okResult.Value;
            Assert.IsTrue(isFollowing);
        }

        private async Task<int> GetFollowedTradersCount(int followerId)
        {
            var followedTraders = await _context.Follows.Where(f => f.FollowerId == followerId).ToListAsync();
            return followedTraders.Count;
        }
    }
}
