using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FlexFit.Payment.Application.DTOs.Payment;
using FlexFit.Payment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace FlexFit.Payment.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy UserId trong Token.");
            return Guid.Parse(userIdStr);
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackages()
        {
            try
            {
                var result = await _paymentService.GetPackagesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _paymentService.CreatePaymentUrlAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("callback")]
        [HttpGet("callback")]
        public async Task<IActionResult> PaymentCallback([FromQuery] Guid paymentId, [FromQuery] string status, [FromQuery] string? providerTransactionCode, [FromQuery] string? message)
        {
            try
            {
                var callbackRequest = new PaymentCallbackRequest
                {
                    PaymentId = paymentId,
                    Status = status,
                    ProviderTransactionCode = providerTransactionCode,
                    Message = message
                };

                var isSuccess = await _paymentService.ProcessPaymentCallbackAsync(callbackRequest);
                if (isSuccess)
                {
                    return Ok(new { Message = "Xử lý thanh toán thành công!", Status = "Success" });
                }
                
                return BadRequest(new { Message = "Xử lý thanh toán thất bại hoặc giao dịch đã được xử lý.", Status = "Failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook([FromBody] Webhook request)
        {
            try
            {
                var result = await _paymentService.ProcessPayOSWebhookAsync(request);
                return Ok(new { Message = result ? "Xử lý PayOS Webhook thành công!" : "Webhook đã được ghi nhận.", Success = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS Webhook Error] {ex.Message}");
                return Ok(new { Message = "Webhook đã được ghi nhận.", Success = false, Error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-credit")]
        public async Task<IActionResult> GetMyCredit()
        {
            try
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetMyPaymentHistory()
        {
            try
            {
                var userId = GetUserId();
                var history = await _paymentService.GetUserPaymentHistoryAsync(userId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{paymentId}/status")]
        public async Task<IActionResult> GetPaymentStatus(Guid paymentId)
        {
            try
            {
                var userId = GetUserId();
                var status = await _paymentService.GetPaymentStatusAsync(paymentId, userId);
                if (status == null)
                {
                    return NotFound(new { Message = "Không tìm thấy giao dịch." });
                }
                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/history")]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var history = await _paymentService.GetAllPaymentsAsync();
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
