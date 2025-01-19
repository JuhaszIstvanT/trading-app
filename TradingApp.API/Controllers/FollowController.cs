using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingApp.API.Models.DTO;
using TradingApp.API.Services;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly FollowService _followService;

        public FollowController(FollowService followService)
        {
            _followService = followService;
        }

        [HttpPost("follow")]
        public async Task<IActionResult> FollowTrader([FromBody] FollowRequestDto request)
        {
            try
            {
                await _followService.FollowTrader(request.FollowerId, request.FollowedTraderId);
                return Ok(new { message = "Successfully followed the trader." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("unfollow")]
        public async Task<IActionResult> UnfollowTrader([FromBody] FollowRequestDto request)
        {
            try
            {
                await _followService.UnfollowTrader(request.FollowerId, request.FollowedTraderId);
                return Ok(new { message = "Successfully unfollowed the trader." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("followed-traders/{followerId}")]
        public async Task<IActionResult> GetFollowedTraders(int followerId)
        {
            var followedTraders = await _followService.GetFollowedTraders(followerId);
            return Ok(followedTraders);
        }

        [HttpGet("is-following")]
        public async Task<IActionResult> IsFollowing([FromQuery] int followerId, [FromQuery] int followedTraderId)
        {
            var isFollowing = await _followService.IsFollowing(followerId, followedTraderId);
            return Ok(isFollowing);
        }

        [HttpGet("userId")]
        public IActionResult GetIdForCurrentUser()
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Ok(userId);
        }

    }
}
