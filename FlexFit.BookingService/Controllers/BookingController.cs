using FlexFit.BookingService.DTOs.Requests;
using FlexFit.BookingService.DTOs.Responses;
using FlexFit.BookingService.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize] // Requires JWT validation
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

        [HttpGet("promotion-preview")]
        public async Task<IActionResult> GetPromotionPreview([FromQuery] int originalCredit)
        {
            try
            {
                if (originalCredit < 0)
                {
                    return BadRequest(new { Message = "Chi phí credit không hợp lệ." });
                }

                // Call local promotional checks (catalog or mock config)
                return Ok(new PromotionPreviewResponse
                {
                    OriginalCredit = originalCredit,
                    DiscountPercent = 0,
                    DiscountCredit = 0,
                    FinalCredit = originalCredit,
                    PromotionId = null,
                    PromotionTitle = null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("gym")]
        public async Task<IActionResult> BookGymSession([FromBody] CreateGymBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                GymBookingResponse result = await _bookingService.BookGymSessionAsync(userId, request);
                return Ok(new { Message = $"Đặt lịch Gym Session thành công. Đang xử lý thanh toán {result.CreditUsed} credit.", Data = result });
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
                GymBookingResponse result = await _bookingService.CancelGymBookingAsync(userId, bookingId);
                return Ok(new { Message = "Huỷ lịch Gym Session thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("class")]
        public async Task<IActionResult> BookClass([FromBody] CreateClassBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                ClassBookingResponse result = await _bookingService.BookClassAsync(userId, request);
                return Ok(new { Message = $"Đặt lịch Class thành công. Đang xử lý thanh toán {result.CreditUsed} credit.", Data = result });
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
                ClassBookingResponse result = await _bookingService.CancelClassBookingAsync(userId, bookingId);
                return Ok(new { Message = "Huỷ lịch Class thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("staff/check-in")]
        [Authorize(Roles = "Staff,GymPartner")]
        public async Task<IActionResult> GetStaffCheckInBookings()
        {
            try
            {
                var staffId = GetUserId();
                var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
                var result = await _bookingService.GetStaffCheckInBookingsAsync(staffId, role);
                return Ok(result);
            }
            catch (Exception ex) 
            { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }
    }
}
