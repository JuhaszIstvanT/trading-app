using Microsoft.EntityFrameworkCore;
using TradingApp.API.Models;
using TradingApp.API.Models.DTO;

namespace TradingApp.API.Services
{
    public class FollowService
    {
        private readonly ApplicationDbContext _context;

        public FollowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task FollowTrader(int followerId, int followedTraderId)
        {
            if (await IsFollowing(followerId, followedTraderId))
                throw new Exception("Already following this trader.");

            var follow = new Follow
            {
                FollowerId = followerId,
                FollowedTraderId = followedTraderId,
                FollowedOn = DateTime.UtcNow
            };
            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();
        }

        public async Task UnfollowTrader(int followerId, int followedTraderId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedTraderId == followedTraderId);
            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<TraderDto>> GetFollowedTraders(int followerId)
        {
            List<TraderDto> followedTraders = await _context.Follows
                .Where(f => f.FollowerId == followerId)
                .Join(_context.Users,
                    follow => follow.FollowedTraderId,
                    user => user.Id,
                    (follow, user) => new TraderDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName
                    })
                .ToListAsync();

            return followedTraders;
        }

        public async Task<bool> IsFollowing(int followerId, int followedTraderId)
        {
            return await _context.Follows
                .AnyAsync(f => f.FollowerId == followerId && f.FollowedTraderId == followedTraderId);
        }
    }

}
