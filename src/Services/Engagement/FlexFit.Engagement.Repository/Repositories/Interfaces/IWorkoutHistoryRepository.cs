using FlexFit.Engagement.Repository.Models;

namespace FlexFit.Engagement.Repository.Repositories.Interfaces;

public interface IWorkoutHistoryRepository
{
    Task AddAsync(UserWorkoutHistory history);
    Task<UserWorkoutHistory?> GetByIdAsync(Guid historyId);
    Task<IEnumerable<UserWorkoutHistory>> GetByUserIdAsync(Guid userId, DateTime? startDate, DateTime? endDate);
    Task<IReadOnlyList<UserWorkoutHistory>> GetRecentByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<bool> ExistsByClassBookingIdAsync(Guid classBookingId);
    Task<bool> ExistsByGymBookingIdAsync(Guid gymBookingId);
    Task UpdateAsync(UserWorkoutHistory history);
    Task SaveChangesAsync();
}

