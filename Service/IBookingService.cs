using Flexfit.DTOs.Booking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IBookingService
    {
        // --- Gym Session Booking ---
        Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request);
        Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId);

        // ĐÃ ĐỔI: Chuyển từ Task<bool> thành Task<GymBookingResponse>
        Task<GymBookingResponse> CancelGymBookingAsync(Guid userId, Guid bookingId);

        // --- Class Booking ---
        Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request);
        Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId);

        // ĐÃ ĐỔI: Chuyển từ Task<bool> thành Task<ClassBookingResponse>
        Task<ClassBookingResponse> CancelClassBookingAsync(Guid userId, Guid bookingId);

        // --- Partner Bookings ---
        Task<IEnumerable<GymBookingResponse>> GetPartnerGymBookingsAsync(Guid ownerId);
        Task<IEnumerable<ClassBookingResponse>> GetPartnerClassBookingsAsync(Guid ownerId);
    }
}