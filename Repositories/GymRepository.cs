using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class GymRepository : IGymRepository
    {
        private readonly FlexFitDbContext _db;
        public GymRepository(FlexFitDbContext db) => _db = db;

        public async Task<IEnumerable<Gym>> GetAllAsync()
        {
            return await _db.Gyms.ToListAsync();
        }

        public async Task<IEnumerable<Gym>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _db.Gyms.Where(g => g.OwnerId == ownerId).ToListAsync();
        }

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

        // --- TRIỂN KHAI HÀM CHECK SỞ HỮU ---
        public async Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId)
        {
            return await _db.Gyms.AnyAsync(g => g.GymId == gymId && g.OwnerId == userId);
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId)
        {
            return await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        public async Task AddUserRoleAsync(UserRole userRole)
        {
            await _db.UserRoles.AddAsync(userRole);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _db.Users.FindAsync(userId);
        }

        public async Task<int> CountGymsByOwnerIdAsync(Guid ownerId)
        {
            return await _db.Gyms.CountAsync(g => g.OwnerId == ownerId);
        }

        public async Task<IEnumerable<Gym>> GetOwnedGymsExceptAsync(Guid ownerId, Guid excludedGymId)
        {
            return await _db.Gyms
                .Where(g => g.OwnerId == ownerId && g.GymId != excludedGymId)
                .ToListAsync();
        }

        public async Task RemoveUserRoleAsync(Guid userId, Guid roleId)
        {
            var userRole = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole != null)
            {
                _db.UserRoles.Remove(userRole);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
