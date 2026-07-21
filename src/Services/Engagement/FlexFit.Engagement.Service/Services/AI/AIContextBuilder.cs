using FlexFit.Engagement.Service.DTOs.AI;
using FlexFit.Engagement.Repository.Repositories.Interfaces;
using FlexFit.Engagement.Service.Interfaces;

namespace FlexFit.Engagement.Service.Services.AI;

/// <summary>
/// Builds AI context from local Engagement data (workout history)
/// and placeholder data for cross-service info (bookings, profiles).
/// In the future, this will call monolith REST API or gRPC for full context.
/// </summary>
public class AIContextBuilder : IAIContextBuilder
{
    private readonly IEngagementUserRepository _userRepository;
    private readonly IWorkoutHistoryRepository _workoutHistoryRepository;
    private readonly IReviewRepository _reviewRepository;

    public AIContextBuilder(
        IEngagementUserRepository userRepository,
        IWorkoutHistoryRepository workoutHistoryRepository,
        IReviewRepository reviewRepository)
    {
        _userRepository = userRepository;
        _workoutHistoryRepository = workoutHistoryRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<AIUserContextDto> GetUserContextAsync(Guid userId)
    {
        // Fetch user from Engagement DB via Repository
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng này trong hệ thống.");
        }

        var dto = new AIUserContextDto
        {
            UserId = user.UserId,
            UserName = user.Email ?? "",
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            Role = "Member" // Default — will be enriched via API aggregation later
        };

        // Fetch local workout history via Repository
        var histories = await _workoutHistoryRepository.GetRecentByUserIdAsync(userId, 10);

        foreach (var h in histories)
        {
            string type = h.ClassBookingId.HasValue ? "Lớp học" : "Tập tự do phòng Gym";
            dto.WorkoutHistorySummary.Add(
                $"{h.CreatedAt:dd/MM/yyyy} | Thể loại: {type} | Thời lượng: {h.WorkoutDurationMinutes} phút | Calo: {h.CaloriesBurned} kcal");
        }

        // Fetch local reviews summary via Repository
        var reviews = await _reviewRepository.GetRecentByUserIdAsync(userId, 5);

        foreach (var r in reviews)
        {
            dto.RecentBookings.Add(
                $"Đánh giá: {r.Rating}/5 sao | Bình luận: {r.Comment ?? "Không có"} | Ngày: {r.CreatedAt:dd/MM/yyyy}");
        }

        return dto;
    }
}

