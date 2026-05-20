using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface IGymRepository
    {
        Task<IEnumerable<Gym>> GetAllAsync();
        Task<Gym?> GetByIdAsync(Guid id);
        Task AddAsync(Gym gym);
        Task UpdateAsync(Gym gym);
        Task DeleteAsync(Guid id);

        // --- CÁC HÀM BỔ SUNG ---
        Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId); // 👈 Thêm hàm check quyền sở hữu này
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);
        Task AddUserRoleAsync(UserRole userRole);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<int> CountGymsByOwnerIdAsync(Guid ownerId);
        Task RemoveUserRoleAsync(Guid userId, Guid roleId);
        Task SaveChangesAsync();
    }
}