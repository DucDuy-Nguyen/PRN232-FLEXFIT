using FlexFit.Engagement.API.Models.Entities;

namespace FlexFit.Engagement.API.Data.Repositories.Interfaces;

public interface IEngagementUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid userId);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}
