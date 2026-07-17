using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IProcessedMessageRepository
    {
        Task<bool> HasBeenProcessedAsync(Guid messageId);
        Task MarkAsProcessedAsync(Guid messageId);
    }
}
