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
                .Include(c => c.User) // Thông tin hội viên
                .Include(c => c.ScannedByNavigation) // Thông tin nhân viên quét
                .Include(c => c.ClassBooking).ThenInclude(cb => cb!.Class) // Thông tin lớp học (nếu có)
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
    }
}