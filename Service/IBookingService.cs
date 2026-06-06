using Flexfit.DTOs;
using Flexfit.DTOs.Booking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IBookingService
    {
        // ========================================================
        // 1. GYM SESSION BOOKING (MEMBER)
        // ========================================================
        Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request);
        Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId);
        Task<GymBookingResponse> CancelGymBookingAsync(Guid userId, Guid bookingId);

        // ========================================================
        // 2. CLASS BOOKING (MEMBER)
        // ========================================================
        Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request);
        Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId);
        Task<ClassBookingResponse> CancelClassBookingAsync(Guid userId, Guid bookingId);

        // ========================================================
        // 3. PARTNER / STAFF METHODS (MANAGEMENT)
        // ========================================================
        Task<IEnumerable<GymBookingResponse>> GetPartnerGymBookingsAsync(Guid ownerId);
        Task<IEnumerable<ClassBookingResponse>> GetPartnerClassBookingsAsync(Guid ownerId);

        /// <summary>
        /// [Staff/Partner] Lấy danh sách lịch đặt phòng Gym và Class của khách hàng, phân loại theo 3 Tab (Active, Upcoming, Past)
        /// </summary>
        // Thay thế hàm cũ bằng 2 hàm mới này trong IBookingService.cs:
        Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerGymBookingTabsAsync(Guid ownerId);
        Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerClassBookingTabsAsync(Guid ownerId);
    
    }
}