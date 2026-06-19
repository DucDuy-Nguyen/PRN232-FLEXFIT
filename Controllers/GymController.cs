using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/gyms")]
    [ApiController]
    [Authorize] // Khóa mặc định toàn bộ endpoint cần đăng nhập
    public class GymController : ControllerBase
    {
        private readonly IGymService _gymService;

        public GymController(IGymService gymService)
        {
            _gymService = gymService;
        }

        // Hàm bổ trợ lấy nhanh UserId từ chuỗi Claims Token
        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdValue)) return Guid.Empty;
            return Guid.Parse(userIdValue);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllGyms()
        {
            var dtos = await _gymService.GetAllGymsAsync();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGymById(Guid id)
        {
            var dto = await _gymService.GetGymByIdAsync(id);
            if (dto == null) return NotFound(new { message = "Không tìm thấy phòng tập." });
            return Ok(dto);
        }

        [HttpGet("partner")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> GetGymsForPartner()
        {
            var ownerId = GetCurrentUserId();
            var dtos = await _gymService.GetGymsByPartnerIdAsync(ownerId);
            return Ok(dtos);
        }

        // ========================================================
        // TẠO PHÒNG GYM (ADMIN TẠO HỘ OWNER)
        // ========================================================

        [HttpPost]
        [Authorize(Roles = "Admin")] // 🔒 Bắt buộc phải là Admin mới có quyền tạo hộ
        public async Task<IActionResult> CreateGym([FromBody] CreateGymRequest request)
        {
            try
            {
                // Truyền request và ID của Admin thực hiện vào Service
                var gymId = await _gymService.CreateGymAsync(request, GetCurrentUserId());
                return Ok(new { message = "Admin đã tạo phòng tập và tự động cấp quyền GymPartner cho Chủ phòng thành công!", gymId });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> UpdateGym(Guid id, [FromBody] UpdateGymRequest request)
        {
            try
            {
                await _gymService.UpdateGymAsync(id, request, GetCurrentUserId());
                return Ok(new { message = "Cập nhật thông tin phòng tập thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Partner,GymPartner")]
        public async Task<IActionResult> ChangeGymStatus(Guid id, [FromBody] string status)
        {
            try
            {
                bool isAdmin = User.IsInRole("Admin");
                await _gymService.ChangeGymStatusAsync(id, status, GetCurrentUserId(), isAdmin);
                return Ok(new { message = $"Đã chuyển trạng thái phòng tập thành: {status}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> DeleteGym(Guid id)
        {
            try
            {
                await _gymService.DeleteGymAsync(id, GetCurrentUserId());
                return Ok(new { message = "Xóa phòng tập thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPut("transfer-owner")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> TransferGymOwnership([FromBody] TransferGymOwnershipDto request)
        {
            try
            {
                await _gymService.TransferGymOwnershipAsync(request, GetCurrentUserId());
                return Ok(new { message = "Đã chuyển nhượng quyền sở hữu phòng tập thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}