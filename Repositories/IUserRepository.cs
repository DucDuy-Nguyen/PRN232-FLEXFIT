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
    }
}