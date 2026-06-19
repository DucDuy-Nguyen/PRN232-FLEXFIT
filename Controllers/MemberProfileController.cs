using Flexfit.DTOs.MemberProfile;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/profiles")]
    [ApiController]
    [Authorize] // Bắt buộc phải có token đăng nhập hợp lệ để xem/sửa profile
    public class MemberProfileController : ControllerBase
    {
        private readonly IMemberProfileService _profileService;

        public MemberProfileController(IMemberProfileService profileService)
        {
            _profileService = profileService;
        }

        // Hàm tiện ích lấy nhanh UserId từ chuỗi Claims Token mã hóa giống BookingController cũ của bạn
        private Guid GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy thông tin nhận diện người dùng trong Token.");
            return Guid.Parse(userIdStr);
        }

        /// <summary>
        /// Lấy thông tin hồ sơ của cá nhân đang đăng nhập
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = GetUserId();
                var result = await _profileService.GetProfileByUserIdAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật hồ sơ cá nhân
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMemberProfileRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _profileService.UpsertProfileAsync(userId, request);
                return Ok(new { Message = "Cập nhật hồ sơ thành công!", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}