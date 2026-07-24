using FlexFit.Booking.Service.DTOs.Requests;
using FlexFit.Booking.Service.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Booking.Service.Service.Interfaces
{
    public interface ICheckInService
    {
        Task<IEnumerable<CheckInLogResponse>> GetAllLogsAsync();
        Task<IEnumerable<CheckInLogResponse>> GetLogsByUserIdAsync(Guid userId);
        Task<CheckInLogResponse> CheckInGymAsync(CheckInGymRequest request, Guid staffId);
        Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId);
        Task<IEnumerable<CheckInLogResponse>> GetManagedLogsAsync(Guid currentUserId, string role);
    }
}
