using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Repositories.Interfaces
{
    public interface IProcessedMessageRepository
    {
        Task<bool> HasBeenProcessedAsync(Guid messageId);
        Task MarkAsProcessedAsync(Guid messageId);
    }
}


