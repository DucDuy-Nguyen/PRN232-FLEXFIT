using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/partner-bookings")]
    [ApiController]
    [Authorize(Roles = "Staff,GymPartner")] // Phân quyền dành cho quản lý và nhân viên
    public class PartnerBookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public PartnerBookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy User ID hợp lệ trong Token.");
            return Guid.Parse(userIdStr);
        }

        /// <summary>
        /// [Staff/GymPartner] Lấy danh sách lịch đặt phòng GYM của khách hàng chia theo 3 Tab (Active, Upcoming, Past)
        /// </summary>
        [HttpGet("gym/tabs")]
        public async Task<IActionResult> GetPartnerGymBookingTabs()
        {
            try
            {
                var ownerId = GetCurrentUserId();
                var result = await _bookingService.GetPartnerGymBookingTabsAsync(ownerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// [Staff/GymPartner] Lấy danh sách lịch đặt CLASS (Lớp học) của khách hàng chia theo 3 Tab (Active, Upcoming, Past)
        /// </summary>
        [HttpGet("class/tabs")]
        public async Task<IActionResult> GetPartnerClassBookingTabs()
        {
            try
            {
                var ownerId = GetCurrentUserId();
                var result = await _bookingService.GetPartnerClassBookingTabsAsync(ownerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}