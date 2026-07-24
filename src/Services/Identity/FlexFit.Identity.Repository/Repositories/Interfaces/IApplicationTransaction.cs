using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Repository.Repositories.Interfaces;

public interface IApplicationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
