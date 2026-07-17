using System;
using System.Threading.Tasks;
using FlexFit.Payment.Application.Interfaces;
using FlexFit.Payment.Domain.Entities;
using FlexFit.Payment.Infrastructure.Data;

namespace FlexFit.Payment.Infrastructure.Repositories
{
    public class ProcessedMessageRepository : IProcessedMessageRepository
    {
        private readonly PaymentDbContext _context;

        public ProcessedMessageRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasBeenProcessedAsync(Guid messageId)
        {
            var exists = await _context.ProcessedMessages.FindAsync(messageId);
            return exists != null;
        }

        public async Task MarkAsProcessedAsync(Guid messageId)
        {
            var processed = new ProcessedMessage
            {
                MessageId = messageId,
                ProcessedAt = DateTime.UtcNow
            };
            await _context.ProcessedMessages.AddAsync(processed);
            await _context.SaveChangesAsync();
        }
    }
}
