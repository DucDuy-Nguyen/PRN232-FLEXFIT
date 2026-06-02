using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<Notification?> GetByIdAndUserIdAsync(Guid notificationId, Guid userId);
        Task UpdateAsync(Notification notification);
        Task MarkAllAsReadByUserIdAsync(Guid userId);
        Task SaveChangesAsync();
    }
}