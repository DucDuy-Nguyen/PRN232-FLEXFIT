using FlexFit.Engagement.API.Models.DTOs.Reviews;

namespace FlexFit.Engagement.API.Services.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(Guid userId, CreateReviewRequest request);
    Task<IEnumerable<ReviewResponse>> GetGymReviewsAsync(Guid gymId);
    Task<IEnumerable<ReviewResponse>> GetClassReviewsAsync(Guid classId);
    Task<IEnumerable<ReviewResponse>> GetMyReviewsAsync(Guid userId);
    Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId);
}
