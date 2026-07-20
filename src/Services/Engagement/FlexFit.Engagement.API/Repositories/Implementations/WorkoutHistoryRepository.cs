using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Repositories.Interfaces;
using FlexFit.Engagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.API.Repositories.Implementations;

public class WorkoutHistoryRepository : IWorkoutHistoryRepository
{
    private readonly EngagementDbContext _context;

    public WorkoutHistoryRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserWorkoutHistory history)
    {
        await _context.UserWorkoutHistories.AddAsync(history);
    }

    public async Task<UserWorkoutHistory?> GetByIdAsync(Guid historyId)
    {
        return await _context.UserWorkoutHistories.FirstOrDefaultAsync(h => h.WorkoutHistoryId == historyId);
    }

    public async Task<IEnumerable<UserWorkoutHistory>> GetByUserIdAsync(Guid userId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.UserWorkoutHistories.Where(h => h.UserId == userId);
        if (startDate.HasValue) query = query.Where(h => h.CreatedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(h => h.CreatedAt <= endDate.Value);
        return await query.OrderByDescending(h => h.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<UserWorkoutHistory>> GetRecentByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.UserWorkoutHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByClassBookingIdAsync(Guid classBookingId)
    {
        return await _context.UserWorkoutHistories.AnyAsync(h => h.ClassBookingId == classBookingId);
    }

    public async Task<bool> ExistsByGymBookingIdAsync(Guid gymBookingId)
    {
        return await _context.UserWorkoutHistories.AnyAsync(h => h.GymBookingId == gymBookingId);
    }

    public async Task UpdateAsync(UserWorkoutHistory history)
    {
        _context.UserWorkoutHistories.Update(history);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
