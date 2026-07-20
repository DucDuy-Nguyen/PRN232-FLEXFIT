using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Data.Repositories.Interfaces;
using FlexFit.Engagement.API.Helpers;
using FlexFit.Engagement.API.Models.Entities;
using FlexFit.Engagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FlexFit.Engagement.API.Services.Implementations;

public class SystemLogService : ISystemLogService
{
    private readonly ISystemLogRepository _logRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SystemLogService(ISystemLogRepository logRepository, IHttpContextAccessor httpContextAccessor)
    {
        _logRepository = logRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActionAsync(Guid? userId, string action, string description, string? ipAddress)
    {
        var context = _httpContextAccessor.HttpContext;

        // Auto-resolve IP address if not provided
        if (string.IsNullOrEmpty(ipAddress) && context != null)
        {
            ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
            }
        }

        // Auto-resolve User ID if not provided
        if (!userId.HasValue && context?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? context.User.FindFirst("sub")?.Value
                              ?? context.User.FindFirst("UserId")?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedGuid))
            {
                userId = parsedGuid;
            }
        }

        var log = new SystemLog
        {
            LogId = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _logRepository.AddAsync(log);
        await _logRepository.SaveChangesAsync();
    }

    public async Task<Tuple<IEnumerable<SystemLog>, int>> GetLogsAsync(
        string? searchTerm,
        string? action,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize)
    {
        return await _logRepository.GetPagedLogsAsync(searchTerm, action, startDate, endDate, pageNumber, pageSize);
    }
}
