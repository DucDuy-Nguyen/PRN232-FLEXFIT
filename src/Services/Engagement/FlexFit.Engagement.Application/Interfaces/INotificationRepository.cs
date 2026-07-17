using FlexFit.Engagement.Domain.Entities;

namespace FlexFit.Engagement.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
    Task<Notification?> GetByIdAndUserIdAsync(Guid notificationId, Guid userId);
    Task UpdateAsync(Notification notification);
    Task MarkAllAsReadByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
}
