using FlexFit.Engagement.Infrastructure.Data;
using FlexFit.Engagement.Domain.Repositories;
using FlexFit.Engagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.Infrastructure.Repositories;

public class EngagementUserRepository : IEngagementUserRepository
{
    private readonly EngagementDbContext _context;

    public EngagementUserRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
