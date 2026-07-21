using System;
using System.Threading.Tasks;
using FlexFit.Payment.Service.DTOs.Responses;
using FlexFit.Payment.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace FlexFit.Payment.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentCallbackController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentCallbackController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> PaymentCallbackGet(
            [FromQuery] Guid paymentId, 
            [FromQuery] string status, 
            [FromQuery] string? providerTransactionCode, 
            [FromQuery] string? message)
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

        [HttpPost("callback")]
        public async Task<IActionResult> PaymentCallbackPost(
            [FromQuery] Guid paymentId, 
            [FromQuery] string status, 
            [FromQuery] string? providerTransactionCode, 
            [FromQuery] string? message)
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

        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook([FromBody] Webhook request)
        {
            try
            {
                var result = await _paymentService.ProcessPayOSWebhookAsync(request);
                // Return success response to PayOS to prevent repeated calls
                return Ok(new { Message = "Webhook đã được xử lý.", Success = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS Webhook Error] {ex.Message}");
                // Return 200/Ok to PayOS even on verification failure to stop retries if appropriate, 
                // or return appropriate response. PayOS webhook requirements say:
                return Ok(new { Message = "Webhook đã được ghi nhận.", Success = false, Error = ex.Message });
            }
        }
    }
}
