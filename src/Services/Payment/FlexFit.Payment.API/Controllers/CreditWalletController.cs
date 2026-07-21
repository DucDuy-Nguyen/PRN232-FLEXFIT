using System;
using System.Threading.Tasks;
using FlexFit.Payment.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexFit.Payment.API.Controllers
{
    [ApiController]
    public class CreditWalletController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public CreditWalletController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        [Authorize]
        [HttpGet("api/users/{userId}/credit-wallet")]
        public async Task<IActionResult> GetUserCreditWallet(Guid userId)
        {
            var response = await _creditService.GetUserCreditAsync(userId);
            if (response == null)
            {
                return Ok(new
                {
                    userId = userId,
                    balance = 0,
                    totalEarned = 0,
                    totalSpent = 0,
                    updatedAt = DateTime.UtcNow
                });
            }
            return Ok(response);
        }

        [Authorize]
        [HttpGet("api/users/{userId}/credit-transactions")]
        public async Task<IActionResult> GetUserTransactionHistory(Guid userId)
        {
            var responses = await _creditService.GetUserTransactionHistoryAsync(userId);
            return Ok(responses);
        }
    }
}
