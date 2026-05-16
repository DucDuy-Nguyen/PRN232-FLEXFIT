using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class GymRepository : IGymRepository
    {
        private readonly FlexFitDbContext _db;
        public GymRepository(FlexFitDbContext db) => _db = db;

        public async Task<IEnumerable<Gym>> GetAllAsync() => await _db.Gyms.ToListAsync();

        public async Task<Gym?> GetByIdAsync(Guid id) => await _db.Gyms.FindAsync(id);

        public async Task AddAsync(Gym gym)
        {
            await _db.Gyms.AddAsync(gym);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Gym gym)
        {
            _db.Gyms.Update(gym);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var gym = await _db.Gyms.FindAsync(id);
            if (gym != null)
            {
                _db.Gyms.Remove(gym);
                await _db.SaveChangesAsync();
            }
        }
    }
}