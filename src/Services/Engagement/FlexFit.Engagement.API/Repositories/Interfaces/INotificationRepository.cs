using FlexFit.Engagement.API.Models;

namespace FlexFit.Engagement.API.Repositories.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid notificationId);
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task UpdateAsync(Notification notification);
    Task SaveChangesAsync();
}
