using FlexFit.Engagement.Infrastructure.Data;
using FlexFit.Engagement.Domain.Repositories;
using FlexFit.Engagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly EngagementDbContext _context;

    public ReviewRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
    }

    public async Task<Review?> GetByIdAsync(Guid reviewId)
    {
        return await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
    }

    public async Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Reviews
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByGymIdAsync(Guid gymId)
    {
        return await _context.Reviews
            .Where(r => r.GymId == gymId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByClassIdAsync(Guid classId)
    {
        return await _context.Reviews
            .Where(r => r.ClassId == classId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsByBookingIdAsync(Guid bookingId)
    {
        return await _context.Reviews.AnyAsync(r => r.BookingId == bookingId);
    }

    public async Task<double> GetAverageRatingByGymIdAsync(Guid gymId)
    {
        var reviews = await _context.Reviews.Where(r => r.GymId == gymId).ToListAsync();
        return reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
    }

    public async Task<int> GetReviewCountByGymIdAsync(Guid gymId)
    {
        return await _context.Reviews.CountAsync(r => r.GymId == gymId);
    }

    public async Task DeleteAsync(Review review)
    {
        _context.Reviews.Remove(review);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
