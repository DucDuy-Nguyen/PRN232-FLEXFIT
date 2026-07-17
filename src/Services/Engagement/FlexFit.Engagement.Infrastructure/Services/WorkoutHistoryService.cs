using FlexFit.Engagement.Application.Common;
using FlexFit.Engagement.Application.DTOs.WorkoutHistory;
using FlexFit.Engagement.Application.Interfaces;
using FlexFit.Engagement.Domain.Entities;

namespace FlexFit.Engagement.Infrastructure.Services;

public class WorkoutHistoryService : IWorkoutHistoryService
{
    private readonly IWorkoutHistoryRepository _historyRepo;

    public WorkoutHistoryService(IWorkoutHistoryRepository historyRepo) { _historyRepo = historyRepo; }

    public async Task CreateHistoryFromCheckInAsync(Guid userId, Guid? classBookingId, Guid? gymBookingId, int calories, int durationMinutes)
    {
        if (classBookingId.HasValue && await _historyRepo.ExistsByClassBookingIdAsync(classBookingId.Value)) return;
        if (gymBookingId.HasValue && await _historyRepo.ExistsByGymBookingIdAsync(gymBookingId.Value)) return;

        var history = new UserWorkoutHistory
        {
            WorkoutHistoryId = Guid.NewGuid(),
            UserId = userId,
            ClassBookingId = classBookingId,
            GymBookingId = gymBookingId,
            CaloriesBurned = calories,
            WorkoutDurationMinutes = durationMinutes,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _historyRepo.AddAsync(history);
        await _historyRepo.SaveChangesAsync();
    }

    public async Task<IEnumerable<WorkoutHistoryDto>> GetMyWorkoutHistoryAsync(Guid userId, DateTime? startDate, DateTime? endDate)
    {
        var histories = await _historyRepo.GetByUserIdAsync(userId, startDate, endDate);
        return histories.Select(MapToDto);
    }

    public async Task<WorkoutStatisticsResponse> GetWorkoutStatisticsAsync(Guid userId)
    {
        var histories = (await _historyRepo.GetByUserIdAsync(userId, null, null)).ToList();

        var totalWorkouts = histories.Count;
        var totalGym = histories.Count(h => h.GymBookingId.HasValue);
        var totalClass = histories.Count(h => h.ClassBookingId.HasValue);
        var totalCalories = histories.Sum(h => h.CaloriesBurned ?? 0);
        var totalDuration = histories.Sum(h => h.WorkoutDurationMinutes ?? 0);
        var averageCalories = totalWorkouts > 0 ? Math.Round((double)totalCalories / totalWorkouts, 1) : 0;

        var now = DateTimeHelper.GetVietnamTime();
        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = now.AddDays(-1 * diff).Date;
        var endOfWeek = startOfWeek.AddDays(7);

        var weeklyHistories = histories.Where(h => h.CreatedAt >= startOfWeek && h.CreatedAt < endOfWeek).ToList();
        var weeklyStats = new List<DailyWorkoutStatDto>();
        string[] dayNames = { "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ Nhật" };

        for (int i = 0; i < 7; i++)
        {
            var dayDate = startOfWeek.AddDays(i);
            var dayHistories = weeklyHistories.Where(h => h.CreatedAt.Date == dayDate).ToList();
            weeklyStats.Add(new DailyWorkoutStatDto
            {
                DayOfWeek = dayNames[i],
                WorkoutCount = dayHistories.Count,
                CaloriesBurned = dayHistories.Sum(h => h.CaloriesBurned ?? 0)
            });
        }

        return new WorkoutStatisticsResponse
        {
            TotalWorkouts = totalWorkouts,
            TotalGymSessions = totalGym,
            TotalClassSessions = totalClass,
            TotalCaloriesBurned = totalCalories,
            TotalDurationMinutes = totalDuration,
            AverageCaloriesPerSession = averageCalories,
            WeeklyStats = weeklyStats
        };
    }

    public async Task<WorkoutHistoryDto> UpdateWorkoutStatsAsync(Guid userId, Guid historyId, UpdateWorkoutHistoryRequest request)
    {
        var history = await _historyRepo.GetByIdAsync(historyId)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch sử tập luyện.");

        if (history.UserId != userId)
            throw new KeyNotFoundException("Lịch sử này không thuộc về bạn.");

        history.CaloriesBurned = request.CaloriesBurned;
        history.WorkoutDurationMinutes = request.WorkoutDurationMinutes;

        await _historyRepo.UpdateAsync(history);
        await _historyRepo.SaveChangesAsync();
        return MapToDto(history);
    }

    private static WorkoutHistoryDto MapToDto(UserWorkoutHistory history) => new()
    {
        WorkoutHistoryId = history.WorkoutHistoryId,
        GymBookingId = history.GymBookingId,
        ClassBookingId = history.ClassBookingId,
        CaloriesBurned = history.CaloriesBurned ?? 0,
        WorkoutDurationMinutes = history.WorkoutDurationMinutes ?? 0,
        WorkoutDate = history.CreatedAt
    };
}
