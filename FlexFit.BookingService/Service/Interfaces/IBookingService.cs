using FlexFit.BookingService.DTOs.Requests;
using FlexFit.BookingService.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Service.Interfaces
{
    public interface IBookingService
    {
        Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request);
        Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId);
        Task<GymBookingResponse> CancelGymBookingAsync(Guid userId, Guid bookingId);

        Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request);
        Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId);
        Task<ClassBookingResponse> CancelClassBookingAsync(Guid userId, Guid bookingId);

        Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerGymBookingTabsAsync(Guid ownerId);
        Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerClassBookingTabsAsync(Guid ownerId);
        Task<IEnumerable<StaffCheckInBookingResponse>> GetStaffCheckInBookingsAsync(Guid staffId, string role);
    }
}
