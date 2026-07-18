using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Interfaces.Services
{
    public interface IDistributedLockService
    {
        Task<bool> AcquireLockAsync(string key, string token, TimeSpan expiration);
        Task<bool> ReleaseLockAsync(string key, string token);
    }
}
