using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.Domain.Entities;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IOutboxRepository
    {
        Task QueueEventAsync<T>(string eventType, T eventPayload);
        Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync();
        Task MarkAsProcessedAsync(Guid messageId);
        Task LogErrorAsync(Guid messageId, string error);
    }
}
