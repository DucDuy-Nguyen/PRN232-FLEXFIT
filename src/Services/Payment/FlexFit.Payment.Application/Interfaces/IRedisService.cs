using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IRedisService
    {
        Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiration);
        Task<bool> ReleaseLockAsync(string key, string value);
        Task<bool> IsIdempotentAsync(string key, TimeSpan expiry);
        Task SetWalletBalanceAsync(Guid userId, int balance);
        Task<int?> GetWalletBalanceAsync(Guid userId);
        Task InvalidateWalletBalanceAsync(Guid userId);
        Task PublishToStreamAsync(string streamName, string eventType, string payload, Guid? correlationId = null);
        Task InvalidateRevenueCacheAsync();
    }
}
