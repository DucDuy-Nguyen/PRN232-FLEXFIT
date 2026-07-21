using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Repository.Data;

public sealed class EfApplicationTransaction : IApplicationTransaction
{
    private readonly IDbContextTransaction _dbContextTransaction;

    public EfApplicationTransaction(IDbContextTransaction dbContextTransaction)
    {
        _dbContextTransaction = dbContextTransaction ?? throw new ArgumentNullException(nameof(dbContextTransaction));
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _dbContextTransaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _dbContextTransaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _dbContextTransaction.DisposeAsync();
    }
}
