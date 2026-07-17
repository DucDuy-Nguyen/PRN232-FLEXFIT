using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IDistributedLockService
    {
        Task<bool> AcquireLockAsync(string key, string token, TimeSpan expiration);
        Task<bool> ReleaseLockAsync(string key, string token);
    }
}
