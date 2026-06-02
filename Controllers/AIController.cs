using Flexfit.DTOs.AI;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;

        public AIController(IAIService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// Lấy gợi ý lịch tập và chế độ dinh dưỡng cá nhân từ AI dựa trên thể trạng và lịch sử tập luyện.
        /// </summary>
        [HttpGet("suggest-workout")]
        public async Task<IActionResult> GetWorkoutSuggestion()
        {
            try
            {
                var userId = GetUserId();
                var suggestion = await _aiService.GetWorkoutSuggestionAsync(userId);
                return Ok(suggestion);
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
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo gợi ý luyện tập từ AI.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Lấy gợi ý lớp học thể thao phù hợp dựa trên sở thích và các lớp đang mở.
        /// </summary>
        [HttpGet("suggest-classes")]
        public async Task<IActionResult> GetClassSuggestion()
        {
            try
            {
                var userId = GetUserId();
                var suggestion = await _aiService.GetClassSuggestionAsync(userId);
                return Ok(suggestion);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo gợi ý lớp học từ AI.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Chat trực tiếp với trợ lý tư vấn sức khỏe & dinh dưỡng AI.
        /// </summary>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIChatRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });
                }

                var userId = GetUserId();
                var response = await _aiService.ChatWithAIAsync(userId, request);
                return Ok(new { response });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình trao đổi với AI.", detail = ex.Message });
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
