using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;

        public NotificationService(INotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public async Task SendAsync(Guid userId, string title, string content, string type)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                IsRead = false,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationRepo.AddAsync(notification);
            await _notificationRepo.SaveChangesAsync();
        }

        public async Task<IEnumerable<Notification>> GetMyNotificationsAsync(Guid userId)
        {
            return await _notificationRepo.GetByUserIdAsync(userId);
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var noti = await _notificationRepo.GetByIdAndUserIdAsync(notificationId, userId);
            if (noti != null)
            {
                noti.IsRead = true;
                await _notificationRepo.UpdateAsync(noti);
                await _notificationRepo.SaveChangesAsync();
            }
        }

        // Thực hiện cập nhật hàng loạt qua Repo
        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _notificationRepo.MarkAllAsReadByUserIdAsync(userId);
            await _notificationRepo.SaveChangesAsync();
        }
    }
}