using FlexFit.BookingService.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Controllers
{
    [Route("api/partner-bookings")]
    [ApiController]
    [Authorize(Roles = "Staff,GymPartner")] // Authorized for Partners and Staff
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
