using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Infrastructure.Redis.Interfaces
{
    public interface IIdempotencyService
    {
        Task<bool> IsIdempotentAsync(string key, TimeSpan expiry);
    }
}



