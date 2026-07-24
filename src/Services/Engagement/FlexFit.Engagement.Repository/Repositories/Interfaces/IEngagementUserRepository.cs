using FlexFit.Engagement.Repository.Models;

namespace FlexFit.Engagement.Repository.Repositories.Interfaces;

public interface IEngagementUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid userId);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}

