using FlexFit.Engagement.Application.DTOs.Reviews;
using FlexFit.Engagement.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlexFit.Engagement.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdValue)) throw new UnauthorizedAccessException("Unauthorized.");
        return Guid.Parse(userIdValue);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        try
        {
            var result = await _reviewService.CreateReviewAsync(GetCurrentUserId(), request);
            return Ok(new { message = "Đánh giá của bạn đã được ghi nhận thành công.", data = result });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("gym/{gymId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGymReviews(Guid gymId)
    {
        var reviews = await _reviewService.GetGymReviewsAsync(gymId);
        return Ok(reviews);
    }

    [HttpGet("class/{classId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetClassReviews(Guid classId)
    {
        var reviews = await _reviewService.GetClassReviewsAsync(classId);
        return Ok(reviews);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReviews()
    {
        var reviews = await _reviewService.GetMyReviewsAsync(GetCurrentUserId());
        return Ok(reviews);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var deleted = await _reviewService.DeleteReviewAsync(id, GetCurrentUserId());
        if (!deleted) return NotFound(new { message = "Không tìm thấy đánh giá hoặc không có quyền xóa." });
        return Ok(new { message = "Xóa đánh giá thành công." });
    }
}
