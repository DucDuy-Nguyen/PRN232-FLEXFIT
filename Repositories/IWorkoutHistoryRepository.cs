using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface IWorkoutHistoryRepository
    {
        Task<UserWorkoutHistory?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserWorkoutHistory>> GetByUserIdAsync(Guid userId, DateTime? startDate, DateTime? endDate);
        Task AddAsync(UserWorkoutHistory history);
        Task UpdateAsync(UserWorkoutHistory history);
        Task SaveChangesAsync();
    }
}
