using Flexfit.Models;
using System;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface IMemberProfileRepository
    {
        Task<MemberProfile?> GetByUserIdAsync(Guid userId);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task AddProfileAsync(MemberProfile profile);
        Task UpdateProfileAsync(MemberProfile profile);
        Task UpdateUserAsync(User user);
        Task SaveChangesAsync();
    }
}