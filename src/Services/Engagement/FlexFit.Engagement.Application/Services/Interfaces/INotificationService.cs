using FlexFit.Engagement.Domain.Entities;
using FlexFit.Engagement.Application.DTOs.Notifications;

namespace FlexFit.Engagement.Application.Services.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string content, string? type = null);
    Task SendBroadcastAsync(string title, string content, string? type = null);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId);
    Task<IEnumerable<Notification>> GetMyNotificationsAsync(Guid userId);
    Task<bool> SendAdminNotificationAsync(AdminCreateNotificationRequest request);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<bool> MarkAllAsReadAsync(Guid userId);
}
