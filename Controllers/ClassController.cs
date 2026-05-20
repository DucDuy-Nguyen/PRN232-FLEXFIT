using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/classes")]
    [ApiController]
    [Authorize]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassController(IClassService classService)
        {
            _classService = classService;
        }

        // Hỗ trợ bóc tách UserId từ JWT Token nhanh gọn
        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdValue)) return Guid.Empty;
            return Guid.Parse(userIdValue);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllClasses()
        {
            var classes = await _classService.GetAllClassesAsync();
            return Ok(classes);
        }

        [HttpGet("branch/{branchId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetClassesByBranch(Guid branchId)
        {
            var classes = await _classService.GetClassesByBranchAsync(branchId);
            return Ok(classes);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetClassById(Guid id)
        {
            var c = await _classService.GetClassByIdAsync(id);
            if (c == null)
                return NotFound(new { message = "Không tìm thấy lớp học." });
            return Ok(c);
        }

        [HttpPost]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            try
            {
                var classId = await _classService.CreateClassAsync(request, GetCurrentUserId());
                return Ok(new { message = "Tạo lớp học thành công!", classId });
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
                return StatusCode(403, new { message = ex.Message }); // Trả về 403 Forbidden nếu không phải chủ phòng
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassRequest request)
        {
            try
            {
                await _classService.UpdateClassAsync(id, request, GetCurrentUserId());
                return Ok(new { message = "Cập nhật thông tin lớp học thành công!" });
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

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> ChangeClassStatus(Guid id, [FromBody] string status)
        {
            try
            {
                await _classService.ChangeClassStatusAsync(id, status, GetCurrentUserId());
                return Ok(new { message = $"Đã chuyển trạng thái lớp học sang: {status}" });
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            try
            {
                await _classService.DeleteClassAsync(id, GetCurrentUserId());
                return Ok(new { message = "Xóa lớp học thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
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
