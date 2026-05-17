using Flexfit.DTOs.Booking;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Flexfit.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize] // Yêu cầu phải đăng nhập
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy UserId trong Token.");
            return Guid.Parse(userIdStr);
        }

        // --- Gym Bookings ---

        [HttpPost("gym")]
        public async Task<IActionResult> BookGymSession([FromBody] CreateGymBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _bookingService.BookGymSessionAsync(userId, request);
                return Ok(new { Message = "Đặt lịch Gym Session thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("gym/my-bookings")]
        public async Task<IActionResult> GetMyGymBookings()
        {
            try
            {
                var userId = GetUserId();
                var result = await _bookingService.GetMyGymBookingsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("gym/{bookingId}/cancel")]
        public async Task<IActionResult> CancelGymBooking(Guid bookingId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _bookingService.CancelGymBookingAsync(userId, bookingId);
                return Ok(new { Message = "Huỷ lịch Gym Session thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- Class Bookings ---

        [HttpPost("class")]
        public async Task<IActionResult> BookClass([FromBody] CreateClassBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _bookingService.BookClassAsync(userId, request);
                return Ok(new { Message = "Đặt lịch Class thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("class/my-bookings")]
        public async Task<IActionResult> GetMyClassBookings()
        {
            try
            {
                var userId = GetUserId();
                var result = await _bookingService.GetMyClassBookingsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("class/{bookingId}/cancel")]
        public async Task<IActionResult> CancelClassBooking(Guid bookingId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _bookingService.CancelClassBookingAsync(userId, bookingId);
                return Ok(new { Message = "Huỷ lịch Class thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- Partner Bookings ---

        [HttpGet("partner/gym")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> GetPartnerGymBookings()
        {
            try
            {
                var ownerId = GetUserId();
                var result = await _bookingService.GetPartnerGymBookingsAsync(ownerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("partner/class")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> GetPartnerClassBookings()
        {
            try
            {
                var ownerId = GetUserId();
                var result = await _bookingService.GetPartnerClassBookingsAsync(ownerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
