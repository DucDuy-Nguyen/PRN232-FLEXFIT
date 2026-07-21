using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FlexFit.Payment.API.DTOs.Requests;
using FlexFit.Payment.API.DTOs.Responses;
using FlexFit.Payment.API.Services.Interfaces;
using FlexFit.Payment.API.Infrastructure.Redis.Interfaces;
using FlexFit.Payment.API.Gateways.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexFit.Payment.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr)) 
                throw new UnauthorizedAccessException("Không tìm thấy UserId trong Token.");
            return Guid.Parse(userIdStr);
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackages()
        {
            var result = await _paymentService.GetPackagesAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var userId = GetUserId();
            var result = await _paymentService.CreatePaymentUrlAsync(userId, request);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("my-credit")]
        public async Task<IActionResult> GetMyCredit()
        {
            var userId = GetUserId();
            var credit = await _paymentService.GetUserCreditAsync(userId);
            if (credit == null)
            {
                return Ok(new { Balance = 0, TotalEarned = 0, TotalSpent = 0 });
            }
            return Ok(new
            {
                Balance = credit.Balance,
                TotalEarned = credit.TotalEarned,
                TotalSpent = credit.TotalSpent,
                UpdatedAt = credit.UpdatedAt
            });
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetMyPaymentHistory()
        {
            var userId = GetUserId();
            var history = await _paymentService.GetUserPaymentHistoryAsync(userId);
            return Ok(history);
        }

        [Authorize]
        [HttpGet("{paymentId}/status")]
        public async Task<IActionResult> GetPaymentStatus(Guid paymentId)
        {
            var userId = GetUserId();
            var status = await _paymentService.GetPaymentStatusAsync(paymentId, userId);
            if (status == null)
            {
                return NotFound(new { Message = "Không tìm thấy giao dịch hoặc không có quyền truy cập." });
            }
            return Ok(status);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/history")]
        public async Task<IActionResult> GetAllPayments()
        {
            var history = await _paymentService.GetAllPaymentsAsync();
            return Ok(history);
        }
    }
}


