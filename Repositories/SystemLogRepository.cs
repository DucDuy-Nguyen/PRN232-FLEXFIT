using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class SystemLogRepository : GenericRepository<SystemLog>, ISystemLogRepository
    {
        public SystemLogRepository(FlexFitDbContext context) : base(context)
        {
        }

        public async Task<Tuple<IEnumerable<SystemLog>, int>> GetPagedLogsAsync(
            string? searchTerm,
            string? action,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet.Include(s => s.User).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => 
                    (s.Description != null && s.Description.Contains(searchTerm)) || 
                    (s.IpAddress != null && s.IpAddress.Contains(searchTerm)) || 
                    (s.User != null && s.User.FullName != null && s.User.FullName.Contains(searchTerm)) ||
                    (s.User != null && s.User.Email != null && s.User.Email.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(s => s.Action == action);
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt <= endDate.Value);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new Tuple<IEnumerable<SystemLog>, int>(logs, totalCount);
        }
    }
}
