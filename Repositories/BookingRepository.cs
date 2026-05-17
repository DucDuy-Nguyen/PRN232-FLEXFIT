using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly FlexFitDbContext _context;

        public BookingRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        // --- Gym Bookings ---

        public async Task<GymBooking?> GetGymBookingByIdAsync(Guid bookingId)
        {
            return await _context.GymBookings
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                        .ThenInclude(br => br.Gym)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<IEnumerable<GymBooking>> GetGymBookingsByUserIdAsync(Guid userId)
        {
            return await _context.GymBookings
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

        // --- Class Bookings ---

        public async Task<ClassBooking?> GetClassBookingByIdAsync(Guid bookingId)
        {
            return await _context.ClassBookings
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<IEnumerable<ClassBooking>> GetClassBookingsByUserIdAsync(Guid userId)
        {
            return await _context.ClassBookings
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

        public async Task<IEnumerable<GymBooking>> GetGymBookingsByOwnerIdAsync(Guid ownerId)
        {
            return await _context.GymBookings
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
    }
}
