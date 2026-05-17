using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/branches")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBranches()
        {
            var dtos = await _branchService.GetAllBranchesAsync();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(Guid id)
        {
            var dto = await _branchService.GetBranchByIdAsync(id);
            if (dto == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
        {
            var branchId = await _branchService.CreateBranchAsync(request);
            return Ok(new { message = "Tạo chi nhánh thành công!", branchId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchRequest request)
        {
            try
            {
                await _branchService.UpdateBranchAsync(id, request);
                return Ok(new { message = "Cập nhật thông tin chi nhánh thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeBranchStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                await _branchService.ChangeBranchStatusAsync(id, isActive);
                string statusMsg = isActive ? "Hoạt động" : "Tạm ngưng";
                return Ok(new { message = $"Đã chuyển trạng thái chi nhánh thành: {statusMsg}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            try
            {
                await _branchService.DeleteBranchAsync(id);
                return Ok(new { message = "Xóa chi nhánh thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("assign-staff")]
        public async Task<IActionResult> AssignStaffToBranch([FromBody] AssignStaffDto dto)
        {
            try
            {
                await _branchService.AssignStaffToBranchAsync(dto);
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
        }

        [HttpDelete("remove-staff")]
        public async Task<IActionResult> RemoveStaffFromBranch([FromQuery] Guid staffId, [FromQuery] Guid branchId)
        {
            try
            {
                await _branchService.RemoveStaffFromBranchAsync(staffId, branchId);
                return Ok(new { message = "Đã gỡ nhân viên ra khỏi chi nhánh và cập nhật lại quyền hạn tài khoản thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("update-staff")]
        public async Task<IActionResult> UpdateBranchStaff([FromBody] UpdateBranchStaffDto dto)
        {
            try
            {
                await _branchService.UpdateBranchStaffAsync(dto);
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
        }
    }
}