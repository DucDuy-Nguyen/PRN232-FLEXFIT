using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Repository.Repositories.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
