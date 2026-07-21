using FlexFit.Booking.Repository.Data;
using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Booking.Repository.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly BookingDbContext _context;

        public BookingRepository(BookingDbContext context)
        {
            _context = context;
        }

        public async Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId)
        {
            return await _context.GymBookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId)
        {
            return await _context.ClassBookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<object?> GetBookingByCodeAsync(string bookingCode)
        {
            var gymBooking = await _context.GymBookings
                .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
            if (gymBooking != null) return gymBooking;

            var classBooking = await _context.ClassBookings
                .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
            return classBooking;
        }

        public async Task<IEnumerable<GymBooking>> GetUserGymBookingsAsync(Guid userId)
        {
            return await _context.GymBookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClassBooking>> GetUserClassBookingsAsync(Guid userId)
        {
            return await _context.ClassBookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsDuplicateGymBookingAsync(Guid userId, Guid sessionId, DateTime date)
        {
            var localDate = date.Date;
            return await _context.GymBookings
                .AnyAsync(b => b.UserId == userId 
                            && b.SessionId == sessionId 
                            && b.StartTimeSnapshot.Date == localDate 
                            && b.Status != "Cancelled" 
                            && b.Status != "Failed");
        }

        public async Task<bool> ExistsDuplicateClassBookingAsync(Guid userId, Guid classId, DateTime date)
        {
            var localDate = date.Date;
            return await _context.ClassBookings
                .AnyAsync(b => b.UserId == userId 
                            && b.ClassId == classId 
                            && b.StartTimeSnapshot.Date == localDate 
                            && b.Status != "Cancelled" 
                            && b.Status != "Failed");
        }

        public async Task<bool> HasOverlappingBookingAsync(Guid userId, DateTime startTime, DateTime endTime)
        {
            var gymOverlap = await _context.GymBookings
                .AnyAsync(b => b.UserId == userId 
                            && b.Status != "Cancelled" 
                            && b.Status != "Failed"
                            && b.StartTimeSnapshot < endTime 
                            && b.EndTimeSnapshot > startTime);

            if (gymOverlap) return true;

            var classOverlap = await _context.ClassBookings
                .AnyAsync(b => b.UserId == userId 
                            && b.Status != "Cancelled" 
                            && b.Status != "Failed"
                            && b.StartTimeSnapshot < endTime 
                            && b.EndTimeSnapshot > startTime);

            return classOverlap;
        }

        public async Task<int> CountActiveClassBookingsAsync(Guid classId)
        {
            return await _context.ClassBookings
                .CountAsync(b => b.ClassId == classId && b.Status != "Cancelled" && b.Status != "Failed");
        }

        public async Task AddGymBookingAsync(GymBooking booking)
        {
            await _context.GymBookings.AddAsync(booking);
        }

        public async Task AddClassBookingAsync(ClassBooking booking)
        {
            await _context.ClassBookings.AddAsync(booking);
        }

        public async Task UpdateGymBookingAsync(GymBooking booking)
        {
            _context.GymBookings.Update(booking);
            await Task.CompletedTask;
        }

        public async Task UpdateClassBookingAsync(ClassBooking booking)
        {
            _context.ClassBookings.Update(booking);
            await Task.CompletedTask;
        }

        public async Task<(List<GymBooking> gymBookings, List<ClassBooking> classBookings)> GetExpiringBookingsAsync(DateTime threshold)
        {
            var gymList = await _context.GymBookings
                .Where(b => b.Status == "PendingCredit" && b.BookedAt <= threshold)
                .ToListAsync();

            var classList = await _context.ClassBookings
                .Where(b => b.Status == "PendingCredit" && b.BookedAt <= threshold)
                .ToListAsync();

            return (gymList, classList);
        }

        public async Task<(List<GymBooking> gymBookings, List<ClassBooking> classBookings)> GetUpcomingUnremindedBookingsAsync(DateTime now, int hoursLeft)
        {
            if (hoursLeft == 3)
            {
                var upperLimit = now.AddHours(3);
                var lowerLimit = now.AddHours(2.75);

                var gymList = await _context.GymBookings
                    .Where(b => b.Status == "Confirmed" 
                                && b.StartTimeSnapshot <= upperLimit 
                                && b.StartTimeSnapshot > lowerLimit 
                                && !b.IsReminded3h)
                    .ToListAsync();

                var classList = await _context.ClassBookings
                    .Where(b => b.Status == "Confirmed" 
                                && b.StartTimeSnapshot <= upperLimit 
                                && b.StartTimeSnapshot > lowerLimit 
                                && !b.IsReminded3h)
                    .ToListAsync();

                return (gymList, classList);
            }
            else
            {
                var upperLimit = now.AddHours(1);

                var gymList = await _context.GymBookings
                    .Where(b => b.Status == "Confirmed" 
                                && b.StartTimeSnapshot <= upperLimit 
                                && b.StartTimeSnapshot > now 
                                && !b.IsReminded1h)
                    .ToListAsync();

                var classList = await _context.ClassBookings
                    .Where(b => b.Status == "Confirmed" 
                                && b.StartTimeSnapshot <= upperLimit 
                                && b.StartTimeSnapshot > now 
                                && !b.IsReminded1h)
                    .ToListAsync();

                return (gymList, classList);
            }
        }

        public async Task<int> GetCancellationCountTodayAsync(Guid userId)
        {
            var today = DateTime.UtcNow.Date;
            var gymCount = await _context.GymBookings
                .CountAsync(b => b.UserId == userId && b.Status == "Cancelled" && b.CancelledAt.HasValue && b.CancelledAt.Value.Date == today);
            var classCount = await _context.ClassBookings
                .CountAsync(b => b.UserId == userId && b.Status == "Cancelled" && b.CancelledAt.HasValue && b.CancelledAt.Value.Date == today);
            return gymCount + classCount;
        }

        public async Task<IEnumerable<GymBooking>> GetGymBookingsByBranchIdsAsync(IEnumerable<Guid> branchIds)
        {
            return await _context.GymBookings
                .Where(b => branchIds.Contains(b.BranchId))
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClassBooking>> GetClassBookingsByBranchIdsAsync(IEnumerable<Guid> branchIds)
        {
            return await _context.ClassBookings
                .Where(b => branchIds.Contains(b.BranchId))
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        // Outbox & Inbox Methods
        public async Task AddOutboxMessageAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
        }

        public async Task AddInboxMessageAsync(InboxMessage message)
        {
            await _context.InboxMessages.AddAsync(message);
        }

        public async Task<bool> InboxMessageExistsAsync(Guid eventId, string consumerName)
        {
            return await _context.InboxMessages
                .AnyAsync(m => m.EventId == eventId && m.ConsumerName == consumerName);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
