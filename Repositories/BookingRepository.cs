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
                .Include(b => b.User) // <-- THÊM: Lấy thông tin User khi lấy chi tiết 1 lịch tập
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<IEnumerable<GymBooking>> GetGymBookingsByUserIdAsync(Guid userId)
        {
            return await _context.GymBookings
                .Include(b => b.User) // <-- THÊM: Lấy thông tin User trong danh sách lịch tập của tôi
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
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
                .Include(b => b.User) // <-- THÊM: Lấy thông tin User khi lấy chi tiết 1 lớp học
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<IEnumerable<ClassBooking>> GetClassBookingsByUserIdAsync(Guid userId)
        {
            return await _context.ClassBookings
                .Include(b => b.User) // <-- THÊM: Lấy thông tin User trong danh sách lớp học của tôi
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
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
            return await _context.GymBookings
                .Include(b => b.User) // <-- THÊM: Để chủ phòng gym biết ai đã đặt lịch tập ở cơ sở của họ
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.Session.Branch.Gym.OwnerId == ownerId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClassBooking>> GetClassBookingsByOwnerIdAsync(Guid ownerId)
        {
            return await _context.ClassBookings
                .Include(b => b.User) // <-- THÊM: Để chủ phòng gym biết ai đã đặt lịch lớp học ở cơ sở của họ
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.Class.Branch.Gym.OwnerId == ownerId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        // THÊM HÀM QUÉT LỊCH GYM
        // ==========================================
        public async Task<IEnumerable<GymBooking>> GetGymBookingsToRemindAsync(DateTime now, int hoursLeft)
        {
            var query = _context.GymBookings
                .Include(b => b.User)     // Include User để lấy Email gửi mail
                .Include(b => b.Session)  // Include Session để lấy StartTime
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
        // THÊM HÀM QUÉT LỊCH LỚP HỌC (CLASS)
        // ==========================================
        public async Task<IEnumerable<ClassBooking>> GetClassBookingsToRemindAsync(DateTime now, int hoursLeft)
        {
            var query = _context.ClassBookings
                .Include(b => b.User)    // Include User để lấy Email gửi mail
                .Include(b => b.Class)   // Include Class để lấy StartTime
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
    }
}
