using System;
using System.Threading.Tasks;
using FlexFit.Payment.API.Repositories.Interfaces;
using FlexFit.Payment.API.Domain.Entities;
using FlexFit.Payment.API.Data;

namespace FlexFit.Payment.API.Repositories.Implementations
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


