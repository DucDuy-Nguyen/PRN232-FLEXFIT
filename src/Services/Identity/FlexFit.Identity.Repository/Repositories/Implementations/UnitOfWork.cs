using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlexFit.Identity.Repository.Data;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Repository.Repositories.Implementations;

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

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async (ct) =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await action();
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}
