using FlexFit.Engagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexFit.Engagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SystemLogController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public SystemLogController(ISystemLogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? searchTerm,
        [FromQuery] string? action,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var (logs, totalCount) = await _logService.GetLogsAsync(
            searchTerm, action, startDate, endDate, pageNumber, pageSize);

        var result = logs.Select(l => new
        {
            l.LogId,
            l.UserId,
            UserEmail = l.User?.Email,
            UserFullName = l.User?.FullName,
            l.Action,
            l.Description,
            l.IpAddress,
            l.CreatedAt
        });

        return Ok(new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Logs = result
        });
    }
}
