using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Repositories.Interfaces;
using FlexFit.Engagement.API.Helpers;
using FlexFit.Engagement.API.DTOs.WorkoutHistory;
using FlexFit.Engagement.API.Models;
using FlexFit.Engagement.API.Services.Interfaces;

namespace FlexFit.Engagement.API.Services.Implementations;

public class WorkoutHistoryService : IWorkoutHistoryService
{
    private readonly IWorkoutHistoryRepository _historyRepo;

    public WorkoutHistoryService(IWorkoutHistoryRepository historyRepo)
    {
        _historyRepo = historyRepo;
    }

    public async Task CreateHistoryFromCheckInAsync(Guid userId, Guid? classBookingId, Guid? gymBookingId, int calories, int durationMinutes)
    {
        if (classBookingId.HasValue)
        {
            var exists = await _historyRepo.ExistsByClassBookingIdAsync(classBookingId.Value);
            if (exists) return;
        }

        if (gymBookingId.HasValue)
        {
            var exists = await _historyRepo.ExistsByGymBookingIdAsync(gymBookingId.Value);
            if (exists) return;
        }

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
        var list = await _historyRepo.GetByUserIdAsync(userId, startDate, endDate);
        return list.Select(h => new WorkoutHistoryDto
        {
            WorkoutHistoryId = h.WorkoutHistoryId,
            GymBookingId = h.GymBookingId,
            ClassBookingId = h.ClassBookingId,
            CaloriesBurned = h.CaloriesBurned ?? 0,
            WorkoutDurationMinutes = h.WorkoutDurationMinutes ?? 0,
            WorkoutDate = h.CreatedAt
        });
    }

    public async Task<WorkoutStatisticsResponse> GetWorkoutStatisticsAsync(Guid userId)
    {
        var list = (await _historyRepo.GetByUserIdAsync(userId, null, null)).ToList();
        var now = DateTimeHelper.GetVietnamTime();
        var oneWeekAgo = now.AddDays(-7);
        var weeklyList = list.Where(h => h.CreatedAt >= oneWeekAgo).ToList();

        var totalGym = list.Count(h => h.GymBookingId.HasValue);
        var totalClass = list.Count(h => h.ClassBookingId.HasValue);
        var totalCalories = list.Sum(h => h.CaloriesBurned ?? 0);
        var totalDuration = list.Sum(h => h.WorkoutDurationMinutes ?? 0);
        var totalWorkouts = list.Count;

        var response = new WorkoutStatisticsResponse
        {
            TotalWorkouts = totalWorkouts,
            TotalGymSessions = totalGym,
            TotalClassSessions = totalClass,
            TotalCaloriesBurned = totalCalories,
            TotalDurationMinutes = totalDuration,
            AverageCaloriesPerSession = totalWorkouts > 0 ? (double)totalCalories / totalWorkouts : 0
        };

        // Group weekly stats by Day of Week
        var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        foreach (var day in days)
        {
            var dayWorkouts = weeklyList.Where(h => h.CreatedAt.ToString("dddd") == day).ToList();
            response.WeeklyStats.Add(new DailyWorkoutStatDto
            {
                DayOfWeek = day,
                WorkoutCount = dayWorkouts.Count,
                CaloriesBurned = dayWorkouts.Sum(h => h.CaloriesBurned ?? 0)
            });
        }

        return response;
    }

    public async Task<WorkoutHistoryDto> UpdateWorkoutStatsAsync(Guid userId, Guid historyId, UpdateWorkoutHistoryRequest request)
    {
        var history = await _historyRepo.GetByIdAsync(historyId)
            ?? throw new KeyNotFoundException("Không tìm thấy dữ liệu tập luyện.");

        if (history.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền sửa đổi dữ liệu này.");

        history.CaloriesBurned = request.CaloriesBurned;
        history.WorkoutDurationMinutes = request.WorkoutDurationMinutes;

        await _historyRepo.UpdateAsync(history);
        await _historyRepo.SaveChangesAsync();

        return new WorkoutHistoryDto
        {
            WorkoutHistoryId = history.WorkoutHistoryId,
            GymBookingId = history.GymBookingId,
            ClassBookingId = history.ClassBookingId,
            CaloriesBurned = history.CaloriesBurned ?? 0,
            WorkoutDurationMinutes = history.WorkoutDurationMinutes ?? 0,
            WorkoutDate = history.CreatedAt
        };
    }
}
