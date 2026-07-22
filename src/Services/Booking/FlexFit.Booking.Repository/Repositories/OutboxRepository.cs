using FlexFit.Booking.Repository.Data;
using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Booking.Repository.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly BookingDbContext _context;

        public OutboxRepository(BookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int limit, CancellationToken cancellationToken)
        {
            return await _context.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .OrderBy(m => m.OccurredAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public Task UpdateOutboxMessageAsync(OutboxMessage message)
        {
            _context.OutboxMessages.Update(message);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
