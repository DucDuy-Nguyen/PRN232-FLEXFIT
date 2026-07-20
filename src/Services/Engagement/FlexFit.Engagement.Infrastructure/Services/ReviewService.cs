using FlexFit.Engagement.Infrastructure.Data;
using FlexFit.Engagement.Domain.Repositories;
using FlexFit.Engagement.Application.Helpers;
using FlexFit.Engagement.Application.DTOs.Reviews;
using FlexFit.Engagement.Domain.Entities;
using FlexFit.Engagement.Application.Services.Interfaces;

namespace FlexFit.Engagement.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepo;

    public ReviewService(IReviewRepository reviewRepo)
    {
        _reviewRepo = reviewRepo;
    }

    public async Task<ReviewResponse> CreateReviewAsync(Guid userId, CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Số sao đánh giá phải nằm trong khoảng từ 1 đến 5.");

        if (!request.GymId.HasValue && !request.ClassId.HasValue)
            throw new ArgumentException("Phải có GymId hoặc ClassId.");

        var alreadyReviewed = await _reviewRepo.ExistsByBookingIdAsync(request.BookingId);
        if (alreadyReviewed)
            throw new InvalidOperationException("Lịch đặt này đã được đánh giá trước đó. Mỗi lịch đặt chỉ được đánh giá 1 lần.");

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            UserId = userId,
            BookingId = request.BookingId,
            GymId = request.GymId,
            ClassId = request.ClassId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();

        return MapToResponse(review);
    }

    public async Task<IEnumerable<ReviewResponse>> GetGymReviewsAsync(Guid gymId)
    {
        var reviews = await _reviewRepo.GetByGymIdAsync(gymId);
        return reviews.Select(MapToResponse);
    }

    public async Task<IEnumerable<ReviewResponse>> GetClassReviewsAsync(Guid classId)
    {
        var reviews = await _reviewRepo.GetByClassIdAsync(classId);
        return reviews.Select(MapToResponse);
    }

    public async Task<IEnumerable<ReviewResponse>> GetMyReviewsAsync(Guid userId)
    {
        var reviews = await _reviewRepo.GetByUserIdAsync(userId);
        return reviews.Select(MapToResponse);
    }

    public async Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review == null || review.UserId != userId) return false;

        await _reviewRepo.DeleteAsync(review);
        await _reviewRepo.SaveChangesAsync();
        return true;
    }

    private static ReviewResponse MapToResponse(Review review) => new()
    {
        ReviewId = review.ReviewId,
        UserId = review.UserId,
        BookingId = review.BookingId,
        GymId = review.GymId,
        ClassId = review.ClassId,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt
    };
}
