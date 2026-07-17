using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfApplicationTransaction(dbTransaction);
    }
}
