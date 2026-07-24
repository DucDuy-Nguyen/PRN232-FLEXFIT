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
    public class CheckInRepository : ICheckInRepository
    {
        private readonly BookingDbContext _context;

        public CheckInRepository(BookingDbContext context)
        {
            _context = context;
        }

        public async Task<GymBooking?> GetGymBookingForCheckInAsync(Guid bookingId)
        {
            return await _context.GymBookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<ClassBooking?> GetClassBookingForCheckInAsync(Guid bookingId)
        {
            return await _context.ClassBookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<bool> HasGymBookingCheckedInAsync(Guid bookingId)
        {
            return await _context.CheckInLogs
                .AnyAsync(l => l.GymBookingId == bookingId && l.Status == "Success");
        }

        public async Task<bool> HasClassBookingCheckedInAsync(Guid bookingId)
        {
            return await _context.CheckInLogs
                .AnyAsync(l => l.ClassBookingId == bookingId && l.Status == "Success");
        }

        public async Task AddCheckInLogAsync(CheckInLog log)
        {
            await _context.CheckInLogs.AddAsync(log);
        }

        public async Task<IEnumerable<CheckInLog>> GetCheckInHistoryAsync(Guid userId)
        {
            return await _context.CheckInLogs
                .Include(c => c.GymBooking)
                .Include(c => c.ClassBooking)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.ScannedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CheckInLog>> GetCheckInHistoryByBranchesAsync(IEnumerable<Guid> branchIds)
        {
            return await _context.CheckInLogs
                .Include(c => c.GymBooking)
                .Include(c => c.ClassBooking)
                .Where(c => 
                    (c.GymBooking != null && branchIds.Contains(c.GymBooking.BranchId))
                    ||
                    (c.ClassBooking != null && branchIds.Contains(c.ClassBooking.BranchId))
                )
                .OrderByDescending(c => c.ScannedAt)
                .ToListAsync();
        }

        public async Task AddOutboxMessageAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
