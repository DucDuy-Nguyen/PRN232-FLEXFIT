using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface ISystemLogRepository : IGenericRepository<SystemLog>
    {
        Task<Tuple<IEnumerable<SystemLog>, int>> GetPagedLogsAsync(
            string? searchTerm,
            string? action,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize);
    }
}
