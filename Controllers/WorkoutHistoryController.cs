using Flexfit.DTOs.WorkoutHistory;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Member")]
    public class WorkoutHistoryController : ControllerBase
    {
        private readonly IWorkoutHistoryService _historyService;

        public WorkoutHistoryController(IWorkoutHistoryService historyService)
        {
            _historyService = historyService;
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử tập luyện cá nhân của hội viên.
        /// Hỗ trợ lọc theo khoảng thời gian nếu cần thiết.
        /// </summary>
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = GetUserId();
                var history = await _historyService.GetMyWorkoutHistoryAsync(userId, startDate, endDate);
                return Ok(history);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy lịch sử tập luyện.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Lấy dữ liệu thống kê tập luyện chi tiết cho Dashboard.
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var userId = GetUserId();
                var statistics = await _historyService.GetWorkoutStatisticsAsync(userId);
                return Ok(statistics);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tính toán thống kê tập luyện.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Hội viên tự chỉnh sửa chỉ số Calo hoặc thời gian tập theo ý muốn (Smartwatch).
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkoutHistory(Guid id, [FromBody] UpdateWorkoutHistoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetUserId();
                var updated = await _historyService.UpdateWorkoutStatsAsync(userId, id, request);
                return Ok(new { message = "Cập nhật chỉ số tập luyện thành công!", data = updated });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật chỉ số tập luyện.", detail = ex.Message });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định danh tính người dùng.");
            }

            return userId;
        }
    }
}
