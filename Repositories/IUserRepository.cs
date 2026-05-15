using Flexfit.Models;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<bool> ExistsByEmailAsync(string email);
        Task SaveChangesAsync();
    }
}