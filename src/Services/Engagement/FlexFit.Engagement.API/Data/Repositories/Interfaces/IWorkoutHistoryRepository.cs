using FlexFit.Engagement.API.Models.Entities;

namespace FlexFit.Engagement.API.Data.Repositories.Interfaces;

public interface IWorkoutHistoryRepository
{
    Task AddAsync(UserWorkoutHistory history);
    Task<UserWorkoutHistory?> GetByIdAsync(Guid historyId);
    Task<IEnumerable<UserWorkoutHistory>> GetByUserIdAsync(Guid userId, DateTime? startDate, DateTime? endDate);
    Task<bool> ExistsByClassBookingIdAsync(Guid classBookingId);
    Task<bool> ExistsByGymBookingIdAsync(Guid gymBookingId);
    Task UpdateAsync(UserWorkoutHistory history);
    Task SaveChangesAsync();
}
