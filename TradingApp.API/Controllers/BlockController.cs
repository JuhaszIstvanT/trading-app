using Microsoft.AspNetCore.Mvc;
using TradingApp.API.Services;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlockController : ControllerBase
    {
        private readonly BlockchainService _blockchainService;

        public BlockController(BlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpGet("blocks")]
        public async Task<IActionResult> GetBlocks()
        {
            try
            {
                var blocks = await _blockchainService.GetBlocksAsync();
                if (blocks == null)
                {
                    return NotFound("Blocks not found.");
                }

                return Ok(blocks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving blocks.");
            }
        }

        [HttpGet("blocks/{id}")]
        public async Task<IActionResult> GetBlockById(int id)
        {
            try
            {
                var block = await _blockchainService.GetBlockByIdAsync(id);
                if (block == null)
                {
                    return NotFound();
                }
                return Ok(block);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the block.");
            }
        }


    }
}