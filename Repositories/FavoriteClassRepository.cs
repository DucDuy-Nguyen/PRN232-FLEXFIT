using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class FavoriteClassRepository : IFavoriteClassRepository
    {
        private readonly FlexFitDbContext _context;

        public FavoriteClassRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        public async Task<FavoriteClass?> GetAsync(Guid userId, Guid classId)
        {
            return await _context.FavoriteClasses
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ClassId == classId);
        }

        public async Task<IEnumerable<FavoriteClass>> GetByUserIdAsync(Guid userId)
        {
            return await _context.FavoriteClasses
                .Include(f => f.Class) // Include thông tin lớp học để hiển thị lên UI
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(FavoriteClass favoriteClass)
        {
            await _context.FavoriteClasses.AddAsync(favoriteClass);
        }

        public void Remove(FavoriteClass favoriteClass)
        {
            _context.FavoriteClasses.Remove(favoriteClass);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}