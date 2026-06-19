using Flexfit.DTOs.Booking;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize] // Yêu cầu phải đăng nhập bằng JWT
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IEmailService _emailService;

        public BookingController(IBookingService bookingService, IEmailService emailService)
        {
            _bookingService = bookingService;
            _emailService = emailService;
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy UserId trong Token.");
            return Guid.Parse(userIdStr);
        }

        // ========================================================
        // 1. GYM BOOKINGS
        // ========================================================

        [HttpGet("promotion-preview")]
        public async Task<IActionResult> GetPromotionPreview([FromQuery] int originalCredit)
        {
            try
            {
                if (originalCredit < 0)
                {
                    return BadRequest(new { Message = "Chi phí credit không hợp lệ." });
                }

                var result = await _bookingService.GetPromotionPreviewAsync(originalCredit);
                return Ok(result);
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

                if (result != null && !string.IsNullOrEmpty(result.UserEmail))
                {
                    // GỌI HÀM MAIL GYM THÀNH CÔNG (ĐÃ THÊM result.EndTime)
                    _ = _emailService.SendGymBookingSuccessEmailAsync(
                        result.UserEmail,
                        result.UserFullName ?? "Hội viên Flexfit",
                        result.SessionName ?? "Lịch tập Gym",
                        result.BranchName ?? "Chi nhánh Flexfit",
                        result.StartTime,
                        result.EndTime,
                        result.BookingCode
                    );
                }

                return Ok(new { Message = $"Đặt lịch Gym Session thành công. Đã dùng {result.CreditUsed} credit.", Data = result });

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
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("gym/{bookingId}/cancel")]
        public async Task<IActionResult> CancelGymBooking(Guid bookingId)
        {
            try
            {
                var userId = GetUserId();
                GymBookingResponse result = await _bookingService.CancelGymBookingAsync(userId, bookingId);

                if (result != null && !string.IsNullOrEmpty(result.UserEmail))
                {
                    // GỌI HÀM MAIL HỦY GYM (ĐÃ THÊM result.EndTime)
                    _ = _emailService.SendGymBookingCancelledEmailAsync(
                        result.UserEmail,
                        result.UserFullName ?? "Hội viên Flexfit",
                        result.SessionName ?? "Lịch tập Gym",
                        result.BranchName ?? "Chi nhánh Flexfit",
                        result.StartTime,
                        result.EndTime,
                        result.BookingCode
                    );
                }

                return Ok(new { Message = "Huỷ lịch Gym Session thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ========================================================
        // 2. CLASS BOOKINGS
        // ========================================================

        [HttpPost("class")]
        public async Task<IActionResult> BookClass([FromBody] CreateClassBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                ClassBookingResponse result = await _bookingService.BookClassAsync(userId, request);

                if (result != null && !string.IsNullOrEmpty(result.UserEmail))
                {
                    // GỌI HÀM MAIL LỚP HỌC THÀNH CÔNG (ĐÃ THÊM result.EndTime)
                    _ = _emailService.SendClassBookingSuccessEmailAsync(
                        result.UserEmail,
                        result.UserFullName ?? "Hội viên Flexfit",
                        result.ClassName ?? "Lớp học",
                        result.BranchName ?? "Chi nhánh Flexfit",
                        result.StartTime,
                        result.EndTime,
                        result.BookingCode
                    );
                }

                return Ok(new { Message = $"Đặt lịch Class thành công. Đã dùng {result.CreditUsed} credit.", Data = result });

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
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("class/{bookingId}/cancel")]
        public async Task<IActionResult> CancelClassBooking(Guid bookingId)
        {
            try
            {
                var userId = GetUserId();
                ClassBookingResponse result = await _bookingService.CancelClassBookingAsync(userId, bookingId);

                if (result != null && !string.IsNullOrEmpty(result.UserEmail))
                {
                    // GỌI HÀM MAIL HỦY LỚP HỌC (ĐÃ THÊM result.EndTime)
                    _ = _emailService.SendClassBookingCancelledEmailAsync(
                        result.UserEmail,
                        result.UserFullName ?? "Hội viên Flexfit",
                        result.ClassName ?? "Lớp học",
                        result.BranchName ?? "Chi nhánh Flexfit",
                        result.StartTime,
                        result.EndTime,
                        result.BookingCode
                    );
                }

                return Ok(new { Message = "Huỷ lịch Class thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ========================================================
        // 3. PARTNER METHODS
        // ========================================================

        [HttpGet("partner/gym")]
        [Authorize(Roles = "Staff,GymPartner")]
        public async Task<IActionResult> GetPartnerGymBookings()
        {
            try
            {
                var ownerId = GetUserId();
                var result = await _bookingService.GetPartnerGymBookingsAsync(ownerId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("partner/class")]
        [Authorize(Roles = "Staff,GymPartner")]
        public async Task<IActionResult> GetPartnerClassBookings()
        {
            try
            {
                var ownerId = GetUserId();
                var result = await _bookingService.GetPartnerClassBookingsAsync(ownerId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("staff/check-in")]
        [Authorize(Roles = "Staff,GymPartner")]
        public async Task<IActionResult> GetStaffCheckInBookings()
        {
            try
            {
                var staffId = GetUserId();
                var result = await _bookingService.GetStaffCheckInBookingsAsync(staffId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

    }
}

