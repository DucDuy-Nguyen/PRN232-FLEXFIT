using FlexFit.Engagement.Domain.Entities;

namespace FlexFit.Engagement.Domain.Repositories;

public interface ISystemLogRepository
{
    Task AddAsync(SystemLog log);
    Task<Tuple<IEnumerable<SystemLog>, int>> GetPagedLogsAsync(
        string? searchTerm,
        string? action,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize);
    Task SaveChangesAsync();
}
