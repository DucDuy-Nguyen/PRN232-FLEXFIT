using FlexFit.Engagement.Domain.Entities;

namespace FlexFit.Engagement.Domain.Repositories;

public interface IEngagementUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid userId);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}
