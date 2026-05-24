using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class CheckInLogRepository : ICheckInLogRepository
    {
        private readonly FlexFitDbContext _context;

        public CheckInLogRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CheckInLog>> GetAllAsync()
        {
            return await _context.CheckInLogs
                .Include(c => c.User)
                .Include(c => c.ScannedByNavigation)
                .Include(c => c.ClassBooking).ThenInclude(cb => cb!.Class)
                .OrderByDescending(c => c.ScannedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CheckInLog>> GetByUserIdAsync(Guid userId)
        {
            return await _context.CheckInLogs
                .Include(c => c.User)
                .Include(c => c.ScannedByNavigation)
                .Include(c => c.ClassBooking).ThenInclude(cb => cb!.Class)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.ScannedAt)
                .ToListAsync();
        }

        public async Task<CheckInLog?> GetByIdAsync(Guid id)
        {
            return await _context.CheckInLogs
                .Include(c => c.User)
                .Include(c => c.ScannedByNavigation)
                .FirstOrDefaultAsync(c => c.CheckInLogId == id);
        }

        public async Task AddAsync(CheckInLog log)
        {
            await _context.CheckInLogs.AddAsync(log);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // =========================================================================
        // KIỂM TRA QUYỀN SCAN CHO GYM BOOKING (Đi qua Session -> Branch)
        // =========================================================================
        public async Task<bool> IsStaffOrOwnerForGymBookingAsync(Guid bookingId, Guid scannerId)
        {
            // ĐƯỜNG ĐI ĐÚNG: GymBookings -> Session -> Branch -> Gym & BranchStaffs
            var booking = await _context.GymBookings
                .Include(gb => gb.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(b => b.Gym)
                .Include(gb => gb.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(b => b.BranchStaffs)
                .FirstOrDefaultAsync(gb => gb.BookingId == bookingId);

            // Kiểm tra dữ liệu liên kết tuyến tính tránh lỗi NullReferenceException
            if (booking == null || booking.Session == null || booking.Session.Branch == null)
                return false;

            var branch = booking.Session.Branch;

            // Tiến hành so khớp ID người quét mã
            bool isOwner = branch.Gym?.OwnerId == scannerId;
            bool isBranchStaff = branch.BranchStaffs?.Any(bs => bs.StaffId == scannerId) ?? false;

            return isOwner || isBranchStaff;
        }

        // =========================================================================
        // KIỂM TRA QUYỀN SCAN CHO CLASS BOOKING (Đi qua Class -> Branch)
        // =========================================================================
        public async Task<bool> IsStaffOrOwnerForClassBookingAsync(Guid bookingId, Guid scannerId)
        {
            // ĐƯỜNG ĐI ĐÚNG: ClassBookings -> Class -> Branch -> Gym & BranchStaffs
            var booking = await _context.ClassBookings
                .Include(cb => cb.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(b => b.Gym)
                .Include(cb => cb.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(b => b.BranchStaffs)
                .FirstOrDefaultAsync(cb => cb.BookingId == bookingId);

            if (booking == null || booking.Class == null || booking.Class.Branch == null)
                return false;

            var branch = booking.Class.Branch;

            // Tiến hành so khớp ID người quét mã
            bool isOwner = branch.Gym?.OwnerId == scannerId;
            bool isBranchStaff = branch.BranchStaffs?.Any(bs => bs.StaffId == scannerId) ?? false;

            return isOwner || isBranchStaff;
        }
        public async Task<IEnumerable<CheckInLog>> GetLogsForManagerAsync(Guid managerId)
        {
            return await _context.CheckInLogs
                .Include(c => c.User)
                .Include(c => c.ScannedByNavigation)
                .Include(c => c.ClassBooking).ThenInclude(cb => cb!.Class)
                .Where(c =>
                    // 1. Nếu lượt check-in thuộc về Gym Booking công ty/chi nhánh đó quản lý
                    (c.GymBooking != null &&
                        (c.GymBooking.Session.Branch.Gym.OwnerId == managerId ||
                         c.GymBooking.Session.Branch.BranchStaffs.Any(bs => bs.StaffId == managerId)))
                    ||
                    // 2. Nếu lượt check-in thuộc về Class Booking lớp học đó quản lý
                    (c.ClassBooking != null &&
                        (c.ClassBooking.Class.Branch.Gym.OwnerId == managerId ||
                         c.ClassBooking.Class.Branch.BranchStaffs.Any(bs => bs.StaffId == managerId)))
                )
                .OrderByDescending(c => c.ScannedAt)
                .ToListAsync();
        }

        public async Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId)
        {
            return await _context.GymBookings.FindAsync(bookingId);
        }

        public async Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId)
        {
            return await _context.ClassBookings.FindAsync(bookingId);
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
    }
}