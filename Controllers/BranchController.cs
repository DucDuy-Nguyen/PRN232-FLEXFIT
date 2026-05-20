using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Flexfit.Controllers
{
    [Route("api/branches")]
    [ApiController]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
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
        public async Task<IActionResult> GetAllBranches()
        {
            var dtos = await _branchService.GetAllBranchesAsync();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBranchById(Guid id)
        {
            var dto = await _branchService.GetBranchByIdAsync(id);
            if (dto == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });
            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
        {
            try
            {
                var branchId = await _branchService.CreateBranchAsync(request, GetCurrentUserId());
                return Ok(new { message = "Tạo chi nhánh thành công!", branchId });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message }); // Trả về 403 Forbidden nếu không phải chủ phòng
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchRequest request)
        {
            try
            {
                await _branchService.UpdateBranchAsync(id, request, GetCurrentUserId());
                return Ok(new { message = "Cập nhật thông tin chi nhánh thành công!" });
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
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> ChangeBranchStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                await _branchService.ChangeBranchStatusAsync(id, isActive, GetCurrentUserId());
                string statusMsg = isActive ? "Hoạt động" : "Tạm ngưng";
                return Ok(new { message = $"Đã chuyển trạng thái chi nhánh thành: {statusMsg}" });
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
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            try
            {
                await _branchService.DeleteBranchAsync(id, GetCurrentUserId());
                return Ok(new { message = "Xóa chi nhánh thành công!" });
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

        [HttpPost("assign-staff")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> AssignStaffToBranch([FromBody] AssignStaffDto dto)
        {
            try
            {
                await _branchService.AssignStaffToBranchAsync(dto, GetCurrentUserId());
                return Ok(new { message = "Bổ nhiệm nhân viên vào làm việc tại chi nhánh thành công!" });
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

        [HttpDelete("remove-staff")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> RemoveStaffFromBranch([FromQuery] Guid staffId, [FromQuery] Guid branchId)
        {
            try
            {
                await _branchService.RemoveStaffFromBranchAsync(staffId, branchId, GetCurrentUserId());
                return Ok(new { message = "Đã gỡ nhân viên ra khỏi chi nhánh và cập nhật lại quyền hạn tài khoản thành công!" });
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

        [HttpPut("update-staff")]
        [Authorize(Roles = "GymPartner")]
        public async Task<IActionResult> UpdateBranchStaff([FromBody] UpdateBranchStaffDto dto)
        {
            try
            {
                await _branchService.UpdateBranchStaffAsync(dto, GetCurrentUserId());
                return Ok(new { message = "Đã tự động thay thế và chuyển giao quyền quản lý chi nhánh thành công!" });
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