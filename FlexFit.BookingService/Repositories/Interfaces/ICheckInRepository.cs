using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.BookingService.Models;

namespace FlexFit.BookingService.Repositories.Interfaces
{
    public interface ICheckInRepository
    {
        Task<GymBooking?> GetGymBookingForCheckInAsync(Guid bookingId);
        Task<ClassBooking?> GetClassBookingForCheckInAsync(Guid bookingId);
        Task<bool> HasGymBookingCheckedInAsync(Guid bookingId);
        Task<bool> HasClassBookingCheckedInAsync(Guid bookingId);
        Task AddCheckInLogAsync(CheckInLog log);
        Task<IEnumerable<CheckInLog>> GetCheckInHistoryAsync(Guid userId);
        Task<IEnumerable<CheckInLog>> GetCheckInHistoryByBranchesAsync(IEnumerable<Guid> branchIds);
        
        // Outbox Messages
        Task AddOutboxMessageAsync(OutboxMessage message);
        
        Task SaveChangesAsync();
    }
}
