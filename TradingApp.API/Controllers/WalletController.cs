using Microsoft.AspNetCore.Mvc;
using TradingApp.API.Services;
using TradingApp.API.Models.DTO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TradingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace TradingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly WalletService _walletService;
        private readonly BlockchainService _blockchainService;

        public WalletController(WalletService walletService, BlockchainService blockchainService)
        {
            _walletService = walletService;
            _blockchainService = blockchainService;
        }

        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances()
        {
            try
            {
                var wallet = await _walletService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                return Ok(wallet.Balances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet balances.");
            }
        }

        [HttpGet("debitcardnumber")]
        public async Task<IActionResult> GetUserDebitCardNumber()
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _walletService.GetUserByIdAsync(userId);

            return Ok(user.DebitCardNumber);
        }


        [HttpGet("address")]
        public async Task<IActionResult> GetAddress()
        {
            try
            {
                var wallet = await _walletService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                return Ok(new { wallet.Address });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet address.");
            }
        }

        [HttpGet("pendingtransactions")]
        public async Task<IActionResult> GetPendingTransactions()
        {
            try
            {
                var wallet = await _walletService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                var transactions = wallet.Transactions.Where(transaction => transaction.IsPending).ToList();

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet transactions.");
            }
        }

        [HttpGet("transactionhistory")]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                var wallet = await _walletService.GetWalletForCurrentUserAsync(User);
                if (wallet == null)
                {
                    return NotFound("Wallet not found for the current user.");
                }

                var transactions = wallet.Transactions.Where(transaction => !transaction.IsPending).ToList();

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving wallet transactions.");
            }
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> TransferFunds([FromBody] TransferDto transferDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                bool isValidTransfer = await _walletService.IsValidTransfer(userId, transferDto.From, transferDto.To,
                    transferDto.Currency,
                    transferDto.Amount);
                if (isValidTransfer)
                {
                    Transaction transaction = new Transaction
                    {
                        Date = DateTime.UtcNow,
                        Type = "Transfer",
                        Currency = transferDto.Currency,
                        Amount = transferDto.Amount,
                        From = transferDto.From,
                        To = transferDto.To,
                        IsPending = true,
                        TransactionFee = transferDto.Amount * 0.27M,
                        IsValid = false,
                        UserId = Convert.ToInt32(userId)
                    };

                    Transaction addedTransaction = await _walletService.AddTransaction(transaction);

                    await _blockchainService.SendTransactionToMiners(addedTransaction);

                    await _blockchainService.SendBlockToMiners();
                }
                else
                {
                    return BadRequest(new { message = "Transfer is not valid" });
                }

                return Ok(new { message = "Transfer successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Transfer failed", error = ex.Message });
            }
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> DepositFunds([FromBody] DepositDto depositDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                await _walletService.DepositFundsAsync(userId, depositDto.Currency, depositDto.Amount);

                return Ok(new { message = "Deposit successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during deposit", error = ex.Message });
            }
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> WithdrawFunds([FromBody] DepositDto withdrawDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var result = await _walletService.WithdrawFundsAsync(userId, withdrawDto.Currency, withdrawDto.Amount);

                if (result)
                {
                    return Ok(new { message = "Withdrawal successful" });
                }
                else
                {
                    return Ok(new { message = "Withdrawal failed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
