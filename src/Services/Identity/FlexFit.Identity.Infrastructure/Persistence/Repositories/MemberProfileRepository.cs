using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Infrastructure.Persistence.Repositories;

public sealed class MemberProfileRepository : IMemberProfileRepository
{
    private readonly IdentityDbContext _context;

    public MemberProfileRepository(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<MemberProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.MemberProfiles
            .FirstOrDefaultAsync(mp => mp.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(MemberProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        await _context.MemberProfiles.AddAsync(profile, cancellationToken);
    }

    public Task UpdateAsync(MemberProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        _context.MemberProfiles.Update(profile);
        return Task.CompletedTask;
    }
}
