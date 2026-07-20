using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.BookingService.Models;

namespace FlexFit.BookingService.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId);
        Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId);
        Task<object?> GetBookingByCodeAsync(string bookingCode);
        Task<IEnumerable<GymBooking>> GetUserGymBookingsAsync(Guid userId);
        Task<IEnumerable<ClassBooking>> GetUserClassBookingsAsync(Guid userId);
        Task<bool> ExistsDuplicateGymBookingAsync(Guid userId, Guid sessionId, DateTime date);
        Task<bool> ExistsDuplicateClassBookingAsync(Guid userId, Guid classId, DateTime date);
        Task<bool> HasOverlappingBookingAsync(Guid userId, DateTime startTime, DateTime endTime);
        Task<int> CountActiveClassBookingsAsync(Guid classId);
        Task AddGymBookingAsync(GymBooking booking);
        Task AddClassBookingAsync(ClassBooking booking);
        Task UpdateGymBookingAsync(GymBooking booking);
        Task UpdateClassBookingAsync(ClassBooking booking);
        Task<(List<GymBooking> gymBookings, List<ClassBooking> classBookings)> GetExpiringBookingsAsync(DateTime threshold);
        Task<(List<GymBooking> gymBookings, List<ClassBooking> classBookings)> GetUpcomingUnremindedBookingsAsync(DateTime now, int hoursLeft);
        Task<int> GetCancellationCountTodayAsync(Guid userId);
        Task<IEnumerable<GymBooking>> GetGymBookingsByBranchIdsAsync(IEnumerable<Guid> branchIds);
        Task<IEnumerable<ClassBooking>> GetClassBookingsByBranchIdsAsync(IEnumerable<Guid> branchIds);
        
        // Outbox & Inbox Methods
        Task AddOutboxMessageAsync(OutboxMessage message);
        Task AddInboxMessageAsync(InboxMessage message);
        Task<bool> InboxMessageExistsAsync(Guid eventId, string consumerName);
        
        Task SaveChangesAsync();
    }
}
