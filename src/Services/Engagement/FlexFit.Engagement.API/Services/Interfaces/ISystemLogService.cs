using FlexFit.Engagement.API.Models;

namespace FlexFit.Engagement.API.Services.Interfaces;

public interface ISystemLogService
{
    Task LogActionAsync(Guid? userId, string action, string description, string? ipAddress);
    Task<Tuple<IEnumerable<SystemLog>, int>> GetLogsAsync(
        string? searchTerm,
        string? action,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize);
}
