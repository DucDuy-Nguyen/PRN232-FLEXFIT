using Flexfit.Models;

namespace Flexfit.Service
{
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
}
