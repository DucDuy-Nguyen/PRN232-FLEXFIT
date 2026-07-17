using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.Application.Abstractions;

public interface IApplicationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
