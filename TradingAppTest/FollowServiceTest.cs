using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingApp.API.Models.DTO;
using TradingApp.API.Models;
using TradingApp.API.Services;

namespace TradingAppTest
{
    [TestClass]
    public class FollowServiceTest
    {
        private FollowService _followService;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _followService = new FollowService(_context);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var users = new List<User>
            {
                new User { Id = 1, UserName = "User1", DebitCardNumber = "5425233430109903" },
                new User { Id = 2, UserName = "User2", DebitCardNumber = "4917484589897107"},
                new User { Id = 3, UserName = "User3", DebitCardNumber = "2223000048410010"}
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task FollowTrader_Successful()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId = 2;

            // Act
            await _followService.FollowTrader(followerId, followedTraderId);

            // Assert
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedTraderId == followedTraderId);

            Assert.IsNotNull(follow);
        }

        [TestMethod]
        public async Task FollowTrader_AlreadyFollowing()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId = 2;

            _context.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowedTraderId = followedTraderId,
                FollowedOn = DateTime.UtcNow
            });
            _context.SaveChanges();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await _followService.FollowTrader(followerId, followedTraderId);
            }, "Already following this trader.");
        }

        [TestMethod]
        public async Task UnfollowTrader_Successful()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId = 2;

            _context.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowedTraderId = followedTraderId,
                FollowedOn = DateTime.UtcNow
            });
            _context.SaveChanges();

            // Act
            await _followService.UnfollowTrader(followerId, followedTraderId);

            // Assert
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedTraderId == followedTraderId);

            Assert.IsNull(follow);
        }

        [TestMethod]
        public async Task IsFollowing_ReturnsTrue()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId = 2;

            _context.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowedTraderId = followedTraderId,
                FollowedOn = DateTime.UtcNow
            });
            _context.SaveChanges();

            // Act
            bool isFollowing = await _followService.IsFollowing(followerId, followedTraderId);

            // Assert
            Assert.IsTrue(isFollowing);
        }

        [TestMethod]
        public async Task IsFollowing_ReturnsFalse()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId = 2;

            // Act
            bool isFollowing = await _followService.IsFollowing(followerId, followedTraderId);

            // Assert
            Assert.IsFalse(isFollowing);
        }

        [TestMethod]
        public async Task GetFollowedTraders_ReturnsFollowedTraders()
        {
            // Arrange
            int followerId = 1;
            int followedTraderId1 = 2;
            int followedTraderId2 = 3;

            _context.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowedTraderId = followedTraderId1,
                FollowedOn = DateTime.UtcNow
            });
            _context.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowedTraderId = followedTraderId2,
                FollowedOn = DateTime.UtcNow
            });
            _context.SaveChanges();

            // Act
            List<TraderDto> followedTraders = await _followService.GetFollowedTraders(followerId);

            // Assert
            Assert.AreEqual(2, followedTraders.Count);
            Assert.IsTrue(followedTraders.Any(t => t.UserId == followedTraderId1));
            Assert.IsTrue(followedTraders.Any(t => t.UserId == followedTraderId2));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
