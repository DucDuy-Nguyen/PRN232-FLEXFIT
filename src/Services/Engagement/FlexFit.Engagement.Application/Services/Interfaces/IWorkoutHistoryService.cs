using FlexFit.Engagement.Application.DTOs.WorkoutHistory;

namespace FlexFit.Engagement.Application.Services.Interfaces;

public interface IWorkoutHistoryService
{
    Task CreateHistoryFromCheckInAsync(Guid userId, Guid? classBookingId, Guid? gymBookingId, int calories, int durationMinutes);
    Task<IEnumerable<WorkoutHistoryDto>> GetMyWorkoutHistoryAsync(Guid userId, DateTime? startDate, DateTime? endDate);
    Task<WorkoutStatisticsResponse> GetWorkoutStatisticsAsync(Guid userId);
    Task<WorkoutHistoryDto> UpdateWorkoutStatsAsync(Guid userId, Guid historyId, UpdateWorkoutHistoryRequest request);
}
