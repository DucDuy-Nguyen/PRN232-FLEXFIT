using FlexFit.Engagement.Repository.Models;

namespace FlexFit.Engagement.Repository.Repositories.Interfaces;

public interface IReviewRepository
{
    Task AddAsync(Review review);
    Task<Review?> GetByIdAsync(Guid reviewId);
    Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId);
    Task<IReadOnlyList<Review>> GetRecentByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<IEnumerable<Review>> GetByGymIdAsync(Guid gymId);
    Task<IEnumerable<Review>> GetByClassIdAsync(Guid classId);
    Task<bool> ExistsByBookingIdAsync(Guid bookingId);
    Task<double> GetAverageRatingByGymIdAsync(Guid gymId);
    Task<int> GetReviewCountByGymIdAsync(Guid gymId);
    Task DeleteAsync(Review review);
    Task SaveChangesAsync();
}

