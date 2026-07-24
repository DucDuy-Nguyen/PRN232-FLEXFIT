using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Repository.Interfaces
{
    public interface IProcessedMessageRepository
    {
        Task<bool> HasBeenProcessedAsync(Guid messageId);
        Task MarkAsProcessedAsync(Guid messageId);
    }
}
