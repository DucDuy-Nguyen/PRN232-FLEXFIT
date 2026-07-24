using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Service.Interfaces
{
    public interface IIdempotencyService
    {
        Task<bool> IsIdempotentAsync(string key, TimeSpan expiry);
    }
}
