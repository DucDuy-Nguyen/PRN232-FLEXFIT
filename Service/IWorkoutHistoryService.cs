using Flexfit.DTOs.WorkoutHistory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IWorkoutHistoryService
    {
        Task CreateHistoryFromCheckInAsync(Guid userId, Guid? classBookingId, Guid? gymBookingId);
        Task<IEnumerable<WorkoutHistoryDto>> GetMyWorkoutHistoryAsync(Guid userId, DateTime? startDate, DateTime? endDate);
        Task<WorkoutStatisticsResponse> GetWorkoutStatisticsAsync(Guid userId);
        Task<WorkoutHistoryDto> UpdateWorkoutStatsAsync(Guid userId, Guid historyId, UpdateWorkoutHistoryRequest request);
    }
}
