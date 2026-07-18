using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Caching;

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
