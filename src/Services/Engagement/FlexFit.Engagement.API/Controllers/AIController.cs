using FlexFit.Engagement.Application.DTOs.AI;
using FlexFit.Engagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlexFit.Engagement.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService) { _aiService = aiService; }

    private Guid GetUserId()
    {
        var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value
                  ?? User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(val) || !Guid.TryParse(val, out var userId))
            throw new UnauthorizedAccessException("Không thể xác định danh tính người dùng.");
        return userId;
    }

    /// <summary>
    /// Lấy gợi ý lịch tập và chế độ dinh dưỡng cá nhân từ AI.
    /// </summary>
    [HttpGet("suggest-workout")]
    public async Task<IActionResult> GetWorkoutSuggestion()
    {
        try
        {
            var suggestion = await _aiService.GetWorkoutSuggestionAsync(GetUserId());
            return Ok(suggestion);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = "Lỗi khi tạo gợi ý AI.", detail = ex.Message }); }
    }

    /// <summary>
    /// Lấy gợi ý lớp học phù hợp dựa trên sở thích.
    /// </summary>
    [HttpGet("suggest-classes")]
    public async Task<IActionResult> GetClassSuggestion()
    {
        try
        {
            var suggestion = await _aiService.GetClassSuggestionAsync(GetUserId());
            return Ok(suggestion);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = "Lỗi khi tạo gợi ý lớp học AI.", detail = ex.Message }); }
    }

    /// <summary>
    /// Chat trực tiếp với trợ lý tư vấn sức khỏe AI.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AIChatRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });

            var response = await _aiService.ChatWithAIAsync(GetUserId(), request);
            return Ok(new { response });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = "Lỗi khi chat với AI.", detail = ex.Message }); }
    }
}
