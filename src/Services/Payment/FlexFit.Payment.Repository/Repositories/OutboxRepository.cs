using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlexFit.Payment.Repository.Interfaces;
using FlexFit.Payment.Repository.Entities;
using FlexFit.Payment.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Payment.Repository.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly PaymentDbContext _context;

        public OutboxRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task QueueEventAsync<T>(string eventType, T eventPayload)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var payloadStr = JsonSerializer.Serialize(eventPayload, options);

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Payload = payloadStr,
                OccurredAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            await _context.OutboxMessages.AddAsync(message);
        }

        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync()
        {
            return await _context.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(Guid messageId)
        {
            var message = await _context.OutboxMessages.FindAsync(messageId);
            if (message != null)
            {
                message.ProcessedAt = DateTime.UtcNow;
                _context.OutboxMessages.Update(message);
                await _context.SaveChangesAsync();
            }
        }

        public async Task LogErrorAsync(Guid messageId, string error)
        {
            var message = await _context.OutboxMessages.FindAsync(messageId);
            if (message != null)
            {
                message.Error = error;
                _context.OutboxMessages.Update(message);
                await _context.SaveChangesAsync();
            }
        }
    }
}
