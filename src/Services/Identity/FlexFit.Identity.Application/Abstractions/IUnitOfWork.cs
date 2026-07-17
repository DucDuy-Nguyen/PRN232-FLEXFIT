namespace FlexFit.Identity.Application.Abstractions;

/// <summary>
/// Unit of work abstraction for coordinating transactions across repositories.
/// A single SaveChangesAsync call commits all pending changes atomically.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
