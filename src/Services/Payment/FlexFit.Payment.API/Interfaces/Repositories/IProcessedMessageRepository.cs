using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Interfaces.Repositories
{
    public interface IProcessedMessageRepository
    {
        Task<bool> HasBeenProcessedAsync(Guid messageId);
        Task MarkAsProcessedAsync(Guid messageId);
    }
}
