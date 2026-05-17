using Flexfit.Models;


namespace Flexfit.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<bool> ExistsByEmailAsync(string email);
        Task SaveChangesAsync();
        Task<User?> GetByVerificationTokenAsync(string token);
        Task UpdateAsync(User user);

        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);

        
        Task DeleteAsync(Guid id);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId);
        Task AddUserRoleAsync(UserRole userRole);
        Task RemoveUserRoleAsync(UserRole userRole);


    }
}