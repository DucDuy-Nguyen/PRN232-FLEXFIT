using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IIdempotencyService
    {
        Task<bool> IsIdempotentAsync(string key, TimeSpan expiry);
    }
}
