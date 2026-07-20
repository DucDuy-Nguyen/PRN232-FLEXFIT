using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Repositories.Interfaces;
using FlexFit.Engagement.API.Helpers;
using FlexFit.Engagement.API.Hubs;
using FlexFit.Engagement.API.Models;
using FlexFit.Engagement.API.DTOs.Notifications;
using FlexFit.Engagement.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FlexFit.Engagement.API.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(INotificationRepository notificationRepo, IHubContext<NotificationHub> hubContext)
    {
        _notificationRepo = notificationRepo;
        _hubContext = hubContext;
    }

    public async Task SendAsync(Guid userId, string title, string content, string? type = null)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Content = content,
            Type = type ?? "SystemAlert",
            IsRead = false,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();

        // Push realtime via SignalR
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
        {
            notification.NotificationId,
            notification.Title,
            notification.Content,
            notification.Type,
            notification.CreatedAt,
            notification.IsRead
        });
    }

    public async Task SendBroadcastAsync(string title, string content, string? type = null)
    {
        var users = await _notificationRepo.GetAllUsersAsync();
        var now = DateTimeHelper.GetVietnamTime();

        var notifications = users.Select(u => new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = u.UserId,
            Title = title,
            Content = content,
            Type = type ?? "SystemAlert",
            IsRead = false,
            CreatedAt = now
        }).ToList();

        foreach (var n in notifications)
        {
            await _notificationRepo.AddAsync(n);
        }
        await _notificationRepo.SaveChangesAsync();

        // Push broadcast realtime via SignalR to all connected users
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
        {
            Title = title,
            Content = content,
            Type = type ?? "SystemAlert",
            CreatedAt = now
        });
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId)
    {
        return await _notificationRepo.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Notification>> GetMyNotificationsAsync(Guid userId)
    {
        return await GetUserNotificationsAsync(userId);
    }

    public async Task<bool> SendAdminNotificationAsync(AdminCreateNotificationRequest request)
    {
        if (request.UserId.HasValue)
        {
            await SendAsync(request.UserId.Value, request.Title, request.Content, request.Type);
        }
        else
        {
            await SendBroadcastAsync(request.Title, request.Content, request.Type);
        }
        return true;
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId) return false;

        notification.IsRead = true;
        await _notificationRepo.UpdateAsync(notification);
        await _notificationRepo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _notificationRepo.GetByUserIdAsync(userId);
        var unread = notifications.Where(n => !n.IsRead).ToList();
        if (unread.Count == 0) return false;

        foreach (var n in unread)
        {
            n.IsRead = true;
            await _notificationRepo.UpdateAsync(n);
        }
        await _notificationRepo.SaveChangesAsync();
        return true;
    }
}
