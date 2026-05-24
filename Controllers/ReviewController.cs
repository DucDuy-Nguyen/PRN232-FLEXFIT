using Flexfit.DTOs.Review;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Member")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Hội viên gửi đánh giá cho một lịch đặt đã hoàn thành check-in.
        /// Mỗi booking chỉ được đánh giá duy nhất 1 lần.
        /// </summary>
        /// <param name="request">Thông tin đánh giá: BookingId, BookingType ("Class" hoặc "Gym"), Rating (1-5), Comment</param>
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            try
            {
                // Lấy UserId từ JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("sub")?.Value
                                  ?? User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Không thể xác định danh tính người dùng." });

                var result = await _reviewService.CreateBookingReviewAsync(userId, request);
                return Ok(new
                {
                    message = "Đánh giá của bạn đã được ghi nhận thành công. Cảm ơn bạn!",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn.", detail = ex.Message });
            }
        }
    }
}
