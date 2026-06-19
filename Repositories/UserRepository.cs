
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
            return await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
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

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Gyms)
                .Include(u => u.BranchStaffs)
                    .ThenInclude(bs => bs.Branch)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Gyms)
                .Include(u => u.BranchStaffs)
                    .ThenInclude(bs => bs.Branch)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                _db.Users.Remove(user);
            }
        }
        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        }

        public async Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId)
        {
            return await _db.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        public async Task AddUserRoleAsync(UserRole userRole)
        {
            await _db.UserRoles.AddAsync(userRole);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveUserRoleAsync(UserRole userRole)
        {
            _db.UserRoles.Remove(userRole);
            await _db.SaveChangesAsync();
        }

    }
}