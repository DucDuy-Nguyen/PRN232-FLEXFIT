using Flexfit.DTOs.CheckInLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface ICheckInLogService
    {
        Task<IEnumerable<CheckInLogResponse>> GetAllLogsAsync();
        Task<IEnumerable<CheckInLogResponse>> GetLogsByUserIdAsync(Guid userId);
        Task<CheckInLogResponse> CheckInGymAsync(CheckInGymRequest request, Guid staffId);
        Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId);
        Task<IEnumerable<CheckInLogResponse>> GetManagedLogsAsync(Guid currentUserId, string role);
    }
}
