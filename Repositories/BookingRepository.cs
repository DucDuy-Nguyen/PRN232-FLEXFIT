using Flexfit.Helpers;
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly FlexFitDbContext _context;

        public BookingRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. GYM BOOKINGS
        // ========================================================

        public async Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId)
        {
            return await _context.GymBookings
                .Include(b => b.User) // <-- THĂM: Láº¥y thĂ´ng tin User khi láº¥y chi tiáº¿t 1 lá»‹ch táº­p
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<IEnumerable<GymBooking>> GetGymBookingsByUserIdAsync(Guid userId)
        {
            return await _context.GymBookings
                .Include(b => b.User) // <-- THĂM: Láº¥y thĂ´ng tin User trong danh sĂ¡ch lá»‹ch táº­p cá»§a tĂ´i
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }
        public async Task<Dictionary<Guid, Guid>> GetGymReviewIdsByBookingIdsAsync(IEnumerable<Guid> bookingIds)
        {
            var ids = bookingIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, Guid>();
            }

            var reviews = await _context.Reviews
                .Where(r => r.GymBookingId.HasValue && ids.Contains(r.GymBookingId.Value))
                .Select(r => new { BookingId = r.GymBookingId.GetValueOrDefault(), r.ReviewId })
                .ToListAsync();

            return reviews
                .GroupBy(r => r.BookingId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.ReviewId).First());
        }
        public async Task<GymSession?> GetGymSessionByIdAsync(Guid sessionId)
        {
            return await _context.GymSessions
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<GymSession?> GetGymSessionByDetailsAsync(Guid branchId, string sessionName, DateTime startTime, DateTime endTime)
        {
            return await _context.GymSessions
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.SessionName == sessionName && s.StartTime == startTime && s.EndTime == endTime);
        }

        public async Task AddGymSessionAsync(GymSession session)
        {
            await _context.GymSessions.AddAsync(session);
        }

        public async Task AddGymBookingAsync(GymBooking booking)
        {
            await _context.GymBookings.AddAsync(booking);
        }

        public Task UpdateGymBookingAsync(GymBooking booking)
        {
            _context.GymBookings.Update(booking);
            return Task.CompletedTask;
        }

        public async Task<int> CountGymBookingsBySessionIdAsync(Guid sessionId)
        {
            return await _context.GymBookings
                .Where(b => b.SessionId == sessionId && b.Status != "Cancelled")
                .CountAsync();
        }

        // ========================================================
        // 2. CLASS BOOKINGS
        // ========================================================

        public async Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId)
        {
            return await _context.ClassBookings
                .Include(b => b.User) // <-- THĂM: Láº¥y thĂ´ng tin User khi láº¥y chi tiáº¿t 1 lá»›p há»c
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<IEnumerable<ClassBooking>> GetClassBookingsByUserIdAsync(Guid userId)
        {
            return await _context.ClassBookings
                .Include(b => b.User) // <-- THĂM: Láº¥y thĂ´ng tin User trong danh sĂ¡ch lá»›p há»c cá»§a tĂ´i
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }
        public async Task<Dictionary<Guid, Guid>> GetClassReviewIdsByBookingIdsAsync(IEnumerable<Guid> bookingIds)
        {
            var ids = bookingIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, Guid>();
            }

            var reviews = await _context.Reviews
                .Where(r => r.ClassBookingId.HasValue && ids.Contains(r.ClassBookingId.Value))
                .Select(r => new { BookingId = r.ClassBookingId.GetValueOrDefault(), r.ReviewId })
                .ToListAsync();

            return reviews
                .GroupBy(r => r.BookingId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.ReviewId).First());
        }
        public async Task<Class?> GetClassByIdAsync(Guid classId)
        {
            return await _context.Classes
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.ClassId == classId);
        }

        public async Task AddClassBookingAsync(ClassBooking booking)
        {
            await _context.ClassBookings.AddAsync(booking);
        }

        public Task UpdateClassBookingAsync(ClassBooking booking)
        {
            _context.ClassBookings.Update(booking);
            return Task.CompletedTask;
        }

        public async Task<int> CountClassBookingsByClassIdAsync(Guid classId)
        {
            return await _context.ClassBookings
                .Where(b => b.ClassId == classId && b.Status != "Cancelled")
                .CountAsync();
        }

        // ========================================================
        // 3. PARTNER METHODS (OWNER)
        // ========================================================

        public async Task<IEnumerable<GymBooking>> GetGymBookingsByOwnerIdAsync(Guid ownerId)
        {
            var isStaff = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == ownerId && ur.Role.RoleName == "Staff");
            var ownsGym = await _context.Gyms.AnyAsync(g => g.OwnerId == ownerId);

            if (isStaff && !ownsGym)

            {
                var branchIds = await _context.BranchStaffs
                    .Where(bs => bs.StaffId == ownerId)
                    .Select(bs => bs.BranchId)
                    .ToListAsync();

                return await _context.GymBookings
                    .Include(b => b.User)
                    .Include(b => b.Session)
                        .ThenInclude(s => s.Branch)
                            .ThenInclude(br => br.Gym)
                    .Where(b => branchIds.Contains(b.Session.BranchId))
                    .OrderByDescending(b => b.BookedAt)
                    .ToListAsync();
            }

            return await _context.GymBookings
                .Include(b => b.User) // <-- THĂM: Äá»ƒ chá»§ phĂ²ng gym biáº¿t ai Ä‘Ă£ Ä‘áº·t lá»‹ch táº­p á»Ÿ cÆ¡ sá»Ÿ cá»§a há»
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.Session.Branch.Gym.OwnerId == ownerId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClassBooking>> GetClassBookingsByOwnerIdAsync(Guid ownerId)
        {
            var isStaff = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == ownerId && ur.Role.RoleName == "Staff");
            var ownsGym = await _context.Gyms.AnyAsync(g => g.OwnerId == ownerId);

            if (isStaff && !ownsGym)

            {
                var branchIds = await _context.BranchStaffs
                    .Where(bs => bs.StaffId == ownerId)
                    .Select(bs => bs.BranchId)
                    .ToListAsync();

                return await _context.ClassBookings
                    .Include(b => b.User)
                    .Include(b => b.Class)
                        .ThenInclude(c => c.Branch)
                            .ThenInclude(br => br.Gym)
                    .Where(b => branchIds.Contains(b.Class.BranchId))
                    .OrderByDescending(b => b.BookedAt)
                    .ToListAsync();
            }

            return await _context.ClassBookings
                .Include(b => b.User) // <-- THĂM: Äá»ƒ chá»§ phĂ²ng gym biáº¿t ai Ä‘Ă£ Ä‘áº·t lá»‹ch lá»›p há»c á»Ÿ cÆ¡ sá»Ÿ cá»§a há»
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.Class.Branch.Gym.OwnerId == ownerId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<GymBooking>> GetStaffGymBookingsForCheckInAsync(Guid staffId)
        {
            var staffBranchIds = await _context.BranchStaffs
                .Where(bs => bs.StaffId == staffId)
                .Select(bs => bs.BranchId)
                .ToListAsync();

            if (staffBranchIds.Count == 0)
            {
                return Enumerable.Empty<GymBooking>();
            }

            return await _context.GymBookings
                .Include(gb => gb.User)
                .Include(gb => gb.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(b => b.Gym)
                .Where(gb => staffBranchIds.Contains(gb.Session.BranchId))
                .Where(gb => gb.Status == "Confirmed")
                .OrderByDescending(gb => gb.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClassBooking>> GetStaffClassBookingsForCheckInAsync(Guid staffId)
        {
            var staffBranchIds = await _context.BranchStaffs
                .Where(bs => bs.StaffId == staffId)
                .Select(bs => bs.BranchId)
                .ToListAsync();

            if (staffBranchIds.Count == 0)
            {
                return Enumerable.Empty<ClassBooking>();
            }

            return await _context.ClassBookings
                .Include(cb => cb.User)
                .Include(cb => cb.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(b => b.Gym)
                .Where(cb => staffBranchIds.Contains(cb.Class.BranchId))
                .Where(cb => cb.Status == "Confirmed")
                .OrderByDescending(cb => cb.BookedAt)
                .ToListAsync();
        }



        public async Task<UserCredit?> GetUserCreditAsync(Guid userId)
        {
            return await _context.UserCredits
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task AddCreditTransactionAsync(CreditTransaction transaction)
        {
            await _context.CreditTransactions.AddAsync(transaction);
        }

        public async Task<Branch?> GetBranchByIdAsync(Guid branchId)
        {
            return await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId);
        }

        public async Task<IEnumerable<Guid>> GetStaffBranchIdsAsync(Guid staffId)
        {
            return await _context.BranchStaffs
                .Where(bs => bs.StaffId == staffId)
                .Select(bs => bs.BranchId)
                .ToListAsync();
        }


        public async Task<IEnumerable<Guid>> GetStaffIdsByBranchIdAsync(Guid branchId)
        {
            return await _context.BranchStaffs
                .Where(bs => bs.BranchId == branchId)
                .Select(bs => bs.StaffId)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        // THĂM HĂ€M QUĂ‰T Lá»CH GYM
        // ==========================================
        public async Task<IEnumerable<GymBooking>> GetGymBookingsToRemindAsync(DateTime now, int hoursLeft)
        {
            var query = _context.GymBookings
                .Include(b => b.User)     // Include User Ä‘á»ƒ láº¥y Email gá»­i mail
                .Include(b => b.Session)  // Include Session Ä‘á»ƒ láº¥y StartTime
                .Where(b => b.Status == "Confirmed");

            if (hoursLeft == 3)
            {
                return await query.Where(b => b.IsReminded3h == false &&
                                              b.Session.StartTime <= now.AddHours(3) &&
                                              b.Session.StartTime > now.AddHours(1))
                                  .ToListAsync();
            }
            else if (hoursLeft == 1)
            {
                return await query.Where(b => b.IsReminded1h == false &&
                                              b.Session.StartTime <= now.AddHours(1) &&
                                              b.Session.StartTime > now)
                                  .ToListAsync();
            }
            return Enumerable.Empty<GymBooking>();
        }

        // ==========================================
        // THĂM HĂ€M QUĂ‰T Lá»CH Lá»P Há»ŒC (CLASS)
        // ==========================================
        public async Task<IEnumerable<ClassBooking>> GetClassBookingsToRemindAsync(DateTime now, int hoursLeft)
        {
            var query = _context.ClassBookings
                .Include(b => b.User)    // Include User Ä‘á»ƒ láº¥y Email gá»­i mail
                .Include(b => b.Class)   // Include Class Ä‘á»ƒ láº¥y StartTime
                .Where(b => b.Status == "Confirmed");

            if (hoursLeft == 3)
            {
                return await query.Where(b => b.IsReminded3h == false &&
                                              b.Class.StartTime <= now.AddHours(3) &&
                                              b.Class.StartTime > now.AddHours(1))
                                  .ToListAsync();
            }
            else if (hoursLeft == 1)
            {
                return await query.Where(b => b.IsReminded1h == false &&
                                              b.Class.StartTime <= now.AddHours(1) &&
                                              b.Class.StartTime > now)
                                  .ToListAsync();
            }
            return Enumerable.Empty<ClassBooking>();
        }

        public async Task<int> GetCancellationCountTodayAsync(Guid userId)
        {
            var localNow = DateTimeHelper.GetVietnamTime();
            var localTodayStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0);
            var localTodayEnd = localTodayStart.AddDays(1);

            var gymCancelledCount = await _context.GymBookings
                .Where(b => b.UserId == userId && b.Status == "Cancelled" && b.CancelledAt >= localTodayStart && b.CancelledAt < localTodayEnd)
                .CountAsync();

            var classCancelledCount = await _context.ClassBookings
                .Where(b => b.UserId == userId && b.Status == "Cancelled" && b.CancelledAt >= localTodayStart && b.CancelledAt < localTodayEnd)
                .CountAsync();

            return gymCancelledCount + classCancelledCount;
        }
    }
}
