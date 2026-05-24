using Flexfit.DTOs.CheckInLog;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/check-in-logs")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập mới được vào
    public class CheckInLogController : ControllerBase
    {
        private readonly ICheckInLogService _checkInService;

        public CheckInLogController(ICheckInLogService checkInService)
        {
            _checkInService = checkInService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy thông tin định danh.");
            return Guid.Parse(userIdStr);
        }

        /// <summary>
        /// [Staff/Admin/GymPartner] Lấy toàn bộ nhật ký check-in của tất cả hội viên
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")] // 👈 Chỉ những quyền quản lý này mới được xem tất cả
        public async Task<IActionResult> GetAllLogsForAdmin()
        {
            var result = await _checkInService.GetAllLogsAsync();
            return Ok(result);
        }

        /// <summary>
        /// [Member] Hội viên tự xem lịch sử check-in cá nhân của chính mình
        /// </summary>
        [HttpGet("my-history")]
        [Authorize(Roles = "Member")] // 👈 Giới hạn chặt chẽ chỉ dành riêng cho tài khoản Member
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

        /// <summary>
        /// [GymPartner/Staff] Thực hiện quét điểm danh cho Lịch tập Gym tự do
        /// </summary>
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// [GymPartner/Staff] Thực hiện quét điểm danh cho Lịch học Lớp (Class)
        /// </summary>
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        /// <summary>
        /// [Staff/Admin/GymPartner] Lấy nhật ký check-in (Admin xem toàn bộ, Staff/Owner chỉ xem theo cơ sở thuộc quyền quản lý)
        /// </summary>
        [HttpGet("manager/all")]
        [Authorize(Roles = "Staff,GymPartner")]
        public async Task<IActionResult> GetLogsForManager()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Trích xuất vai trò (Role) từ Claims Token
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                // Gọi hàm lọc bảo mật thông tin cơ sở mới tạo
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