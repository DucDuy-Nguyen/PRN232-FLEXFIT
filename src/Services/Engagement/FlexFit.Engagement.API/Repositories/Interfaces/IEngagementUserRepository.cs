using FlexFit.Engagement.API.Models;

namespace FlexFit.Engagement.API.Repositories.Interfaces;

public interface IEngagementUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid userId);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}
