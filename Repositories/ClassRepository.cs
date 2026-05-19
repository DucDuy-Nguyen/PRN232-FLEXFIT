using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class ClassRepository : IClassRepository
    {
        private readonly FlexFitDbContext _db;

        public ClassRepository(FlexFitDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Class>> GetAllAsync()
        {
            return await _db.Classes
                .Include(c => c.Branch)
                .Include(c => c.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Class>> GetByBranchIdAsync(Guid branchId)
        {
            return await _db.Classes
                .Include(c => c.Branch)
                .Include(c => c.Category)
                .Where(c => c.BranchId == branchId)
                .ToListAsync();
        }

        public async Task<Class?> GetByIdAsync(Guid id)
        {
            return await _db.Classes
                .Include(c => c.Branch)
                .Include(c => c.Category)
                .Include(c => c.ClassBookings)
                .FirstOrDefaultAsync(c => c.ClassId == id);
        }

        public async Task AddAsync(Class entity)
        {
            await _db.Classes.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Class entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Classes.FindAsync(id);
            if (entity != null)
            {
                _db.Classes.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> BranchExistsAsync(Guid branchId)
        {
            return await _db.Branches.AnyAsync(b => b.BranchId == branchId);
        }

        public async Task<bool> CategoryExistsAsync(Guid categoryId)
        {
            return await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }
    }
}
