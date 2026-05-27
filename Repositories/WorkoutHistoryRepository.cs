using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class WorkoutHistoryRepository : IWorkoutHistoryRepository
    {
        private readonly FlexFitDbContext _context;

        public WorkoutHistoryRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        public async Task<UserWorkoutHistory?> GetByIdAsync(Guid id)
        {
            return await _context.UserWorkoutHistories
                .Include(h => h.ClassBooking)
                    .ThenInclude(cb => cb.Class)
                        .ThenInclude(c => c.Branch)
                            .ThenInclude(b => b.Gym)
                .Include(h => h.GymBooking)
                    .ThenInclude(gb => gb.Session)
                        .ThenInclude(s => s.Branch)
                            .ThenInclude(b => b.Gym)
                .FirstOrDefaultAsync(h => h.WorkoutHistoryId == id);
        }

        public async Task<IEnumerable<UserWorkoutHistory>> GetByUserIdAsync(Guid userId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.UserWorkoutHistories
                .Include(h => h.ClassBooking)
                    .ThenInclude(cb => cb.Class)
                        .ThenInclude(c => c.Branch)
                            .ThenInclude(b => b.Gym)
                .Include(h => h.GymBooking)
                    .ThenInclude(gb => gb.Session)
                        .ThenInclude(s => s.Branch)
                            .ThenInclude(b => b.Gym)
                .Where(h => h.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(h => h.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(h => h.CreatedAt <= endDate.Value);
            }

            return await query
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(UserWorkoutHistory history)
        {
            await _context.UserWorkoutHistories.AddAsync(history);
        }

        public Task UpdateAsync(UserWorkoutHistory history)
        {
            _context.UserWorkoutHistories.Update(history);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
