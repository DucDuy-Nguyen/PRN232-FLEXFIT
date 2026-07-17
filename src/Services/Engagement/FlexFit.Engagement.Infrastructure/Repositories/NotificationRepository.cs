using FlexFit.Engagement.Application.Interfaces;
using FlexFit.Engagement.Domain.Entities;
using FlexFit.Engagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly EngagementDbContext _context;

    public NotificationRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification?> GetByIdAndUserIdAsync(Guid notificationId, Guid userId)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await Task.CompletedTask;
    }

    // Viết câu lệnh cập nhật hàng loạt bằng EF Core (Tối ưu hiệu năng, không cần dùng vòng lặp)
    public async Task MarkAllAsReadByUserIdAsync(Guid userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead == false)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
