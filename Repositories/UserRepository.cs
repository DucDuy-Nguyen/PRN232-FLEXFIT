
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FlexFitDbContext _db;

        public UserRepository(FlexFitDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _db.Users.AddAsync(user);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _db.Users.AnyAsync(u => u.Email == email);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}