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
        // KIá»‚M TRA QUYá»€N SCAN CHO GYM BOOKING (Äi qua Session -> Branch)
        // =========================================================================
        public async Task<bool> IsStaffOrOwnerForGymBookingAsync(Guid bookingId, Guid scannerId)
        {
            // ÄÆ¯á»œNG ÄI ÄĂNG: GymBookings -> Session -> Branch -> Gym & BranchStaffs
            var booking = await _context.GymBookings
                .Include(gb => gb.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(b => b.Gym)
                .Include(gb => gb.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(b => b.BranchStaffs)
                .FirstOrDefaultAsync(gb => gb.BookingId == bookingId);

            // Kiá»ƒm tra dá»¯ liá»‡u liĂªn káº¿t tuyáº¿n tĂ­nh trĂ¡nh lá»—i NullReferenceException
            if (booking == null || booking.Session == null || booking.Session.Branch == null)
                return false;

            var branch = booking.Session.Branch;

            // Tiáº¿n hĂ nh so khá»›p ID ngÆ°á»i quĂ©t mĂ£
            bool isOwner = branch.Gym?.OwnerId == scannerId;
            bool isBranchStaff = branch.BranchStaffs?.Any(bs => bs.StaffId == scannerId) ?? false;

            return isOwner || isBranchStaff;
        }

        // =========================================================================
        // KIá»‚M TRA QUYá»€N SCAN CHO CLASS BOOKING (Äi qua Class -> Branch)
        // =========================================================================
        public async Task<bool> IsStaffOrOwnerForClassBookingAsync(Guid bookingId, Guid scannerId)
        {
            // ÄÆ¯á»œNG ÄI ÄĂNG: ClassBookings -> Class -> Branch -> Gym & BranchStaffs
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

            // Tiáº¿n hĂ nh so khá»›p ID ngÆ°á»i quĂ©t mĂ£
            bool isOwner = branch.Gym?.OwnerId == scannerId;
            bool isBranchStaff = branch.BranchStaffs?.Any(bs => bs.StaffId == scannerId) ?? false;

            return isOwner || isBranchStaff;
        }

        public async Task<IEnumerable<CheckInLog>> GetLogsForManagerAsync(Guid managerId)
        {
            // Bước 1: Lấy danh sách branchId mà manager này có quyền quản lý (bảng nhỏ, rất nhanh)
            var ownedBranchIds = await _context.Branches
                .Where(b => b.Gym.OwnerId == managerId)
                .Select(b => b.BranchId)
                .ToListAsync();

            var staffBranchIds = await _context.BranchStaffs
                .Where(bs => bs.StaffId == managerId)
                .Select(bs => bs.BranchId)
                .ToListAsync();

            var managedBranchIds = ownedBranchIds.Union(staffBranchIds).Distinct().ToList();

            if (managedBranchIds.Count == 0)
                return Enumerable.Empty<CheckInLog>();

            // Bước 2: Lọc CheckInLogs theo branchId — SQL sinh ra IN clause đơn giản, rất hiệu quả
            return await _context.CheckInLogs
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.ScannedByNavigation)
                .Include(c => c.ClassBooking).ThenInclude(cb => cb!.Class)
                .Where(c =>
                    (c.GymBooking != null && managedBranchIds.Contains(c.GymBooking.Session.BranchId))
                    ||
                    (c.ClassBooking != null && managedBranchIds.Contains(c.ClassBooking.Class.BranchId))
                )
                .OrderByDescending(c => c.ScannedAt)
                .ToListAsync();
        }

        // =========================================================================
        // Cáº¬P NHáº¬T: THĂM .INCLUDE() Äá»‚ Láº¤Y THĂ”NG TIN THá»œI GIAN KHĂ”NG Bá» Lá»–I NULL
        // =========================================================================
        public async Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId)
        {
            return await _context.GymBookings
                .Include(b => b.Session) // <-- Náº¡p kĂ¨m Session Ä‘á»ƒ láº¥y StartTime, EndTime á»Ÿ táº§ng Service
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<GymBooking?> FindGymBookingForCheckInAsync(Guid? bookingId, string? bookingCode, string? qrToken)
        {
            var normalizedCode = bookingCode?.Trim();
            var normalizedToken = qrToken?.Trim();

            return await _context.GymBookings
                .Include(b => b.User)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.BranchStaffs)
                .FirstOrDefaultAsync(b =>
                    (bookingId.HasValue && b.BookingId == bookingId.Value) ||
                    (!string.IsNullOrEmpty(normalizedCode) && b.BookingCode == normalizedCode) ||
                    (!string.IsNullOrEmpty(normalizedToken) && b.QrToken == normalizedToken));
        }

        public async Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId)
        {
            return await _context.ClassBookings
                .Include(b => b.Class) // <-- Náº¡p kĂ¨m Class Ä‘á»ƒ láº¥y StartTime, EndTime á»Ÿ táº§ng Service
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
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
