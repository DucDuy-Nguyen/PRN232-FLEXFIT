using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly FlexFitDbContext _db;
        public BranchRepository(FlexFitDbContext db) => _db = db;

        public async Task<IEnumerable<Branch>> GetAllAsync() => await _db.Branches.ToListAsync();

        public async Task<Branch?> GetByIdAsync(Guid id) => await _db.Branches.FindAsync(id);

        public async Task AddAsync(Branch branch)
        {
            await _db.Branches.AddAsync(branch);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Branch branch)
        {
            _db.Branches.Update(branch);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch != null)
            {
                _db.Branches.Remove(branch);
                await _db.SaveChangesAsync();
            }
        }
    }
}