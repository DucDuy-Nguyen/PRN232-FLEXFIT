using FlexFit.Engagement.API.Data.Repositories.Interfaces;
using FlexFit.Engagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.API.Data.Repositories.Implementations;

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

    public async Task<Notification?> GetByIdAsync(Guid notificationId)
    {
        return await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
