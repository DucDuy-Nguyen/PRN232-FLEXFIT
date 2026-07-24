using FlexFit.Engagement.Service.DTOs.WorkoutHistory;
using FlexFit.Engagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlexFit.Engagement.API.Controllers;

[ApiController]
[Route("api/workout-history")]
[Authorize]
public class WorkoutHistoryController : ControllerBase
{
    private readonly IWorkoutHistoryService _historyService;

    public WorkoutHistoryController(IWorkoutHistoryService historyService)
    {
        _historyService = historyService;
    }

    private Guid GetUserId()
    {
        var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value
                  ?? User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(val)) throw new UnauthorizedAccessException("Unauthorized.");
        return Guid.Parse(val);
    }

    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyHistory([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try { return Ok(await _historyService.GetMyWorkoutHistoryAsync(GetUserId(), startDate, endDate)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try { return Ok(await _historyService.GetWorkoutStatisticsAsync(GetUserId())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkoutHistory(Guid id, [FromBody] UpdateWorkoutHistoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var updated = await _historyService.UpdateWorkoutStatsAsync(GetUserId(), id, request);
            return Ok(new { message = "Cập nhật chỉ số tập luyện thành công!", data = updated });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }
}

