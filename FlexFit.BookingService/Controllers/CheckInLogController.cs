using FlexFit.BookingService.DTOs.Requests;
using FlexFit.BookingService.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Controllers
{
    [Route("api/check-in-logs")]
    [ApiController]
    [Authorize] // Requires login
    public class CheckInLogController : ControllerBase
    {
        private readonly ICheckInService _checkInService;

        public CheckInLogController(ICheckInService checkInService)
        {
            _checkInService = checkInService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy thông tin định danh.");
            return Guid.Parse(userIdStr);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllLogsForAdmin()
        {
            var result = await _checkInService.GetAllLogsAsync();
            return Ok(result);
        }

        [HttpGet("my-history")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> GetMyCheckInHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _checkInService.GetLogsByUserIdAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("gym")]
        [Authorize(Roles = "GymPartner,Staff")]
        public async Task<IActionResult> CheckInGym([FromBody] CheckInGymRequest request)
        {
            try
            {
                var staffId = GetCurrentUserId();
                var result = await _checkInService.CheckInGymAsync(request, staffId);
                return Ok(new { Message = "Điểm danh lịch tập Gym thành công!", Data = result });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("class")]
        [Authorize(Roles = "GymPartner,Staff")]
        public async Task<IActionResult> CheckInClass([FromBody] CheckInClassRequest request)
        {
            try
            {
                var staffId = GetCurrentUserId();
                var result = await _checkInService.CheckInClassAsync(request, staffId);
                return Ok(new { Message = "Điểm danh lớp học thành công!", Data = result });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { Message = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("manager/all")]
        [Authorize(Roles = "Staff,GymPartner")]
        public async Task<IActionResult> GetLogsForManager()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var result = await _checkInService.GetManagedLogsAsync(currentUserId, userRole ?? "");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
