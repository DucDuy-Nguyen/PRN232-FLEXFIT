using FlexFit.Booking.Repository.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Booking.Repository.Repositories.Interfaces
{
    public interface IOutboxRepository
    {
        Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int limit, CancellationToken cancellationToken);
        Task UpdateOutboxMessageAsync(OutboxMessage message);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
