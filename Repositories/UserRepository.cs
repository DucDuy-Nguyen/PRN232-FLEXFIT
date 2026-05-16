
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;


namespace Flexfit.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FlexFitDbContext _db;

        public UserRepository(FlexFitDbContext db)
        {
            _db = db;
        }
        public async Task<User?> GetByVerificationTokenAsync(string token)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
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
        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }
    }
}