using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/classes")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassController(IClassService classService)
        {
            _classService = classService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClasses()
        {
            var classes = await _classService.GetAllClassesAsync();
            return Ok(classes);
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetClassesByBranch(Guid branchId)
        {
            var classes = await _classService.GetClassesByBranchAsync(branchId);
            return Ok(classes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassById(Guid id)
        {
            var c = await _classService.GetClassByIdAsync(id);
            if (c == null)
                return NotFound(new { message = "Không tìm thấy lớp học." });
            return Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            try
            {
                var classId = await _classService.CreateClassAsync(request);
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
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassRequest request)
        {
            try
            {
                await _classService.UpdateClassAsync(id, request);
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
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeClassStatus(Guid id, [FromBody] string status)
        {
            try
            {
                await _classService.ChangeClassStatusAsync(id, status);
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
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            try
            {
                await _classService.DeleteClassAsync(id);
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
        }
    }
}
