using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Interfaces.Services
{
    public interface IIdempotencyService
    {
        Task<bool> IsIdempotentAsync(string key, TimeSpan expiry);
    }
}
