using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlexFit.Payment.Application.Interfaces;
using FlexFit.Payment.Domain.Entities;
using FlexFit.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlexFit.Payment.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CreditPackage>> GetActivePackagesAsync()
        {
            return await _context.CreditPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<CreditPackage?> GetPackageByIdAsync(Guid packageId)
        {
            return await _context.CreditPackages.FindAsync(packageId);
        }

        public async Task CreatePaymentAsync(Domain.Entities.Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
        }

        public async Task<Domain.Entities.Payment?> GetPaymentByIdAsync(Guid paymentId)
        {
            return await _context.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<Domain.Entities.Payment?> GetPaymentByTransactionCodeAsync(string providerTransactionCode)
        {
            return await _context.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p => p.ProviderTransactionCode == providerTransactionCode);
        }

        public async Task UpdatePaymentStatusAsync(Guid paymentId, string status, string? providerTransactionCode)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = status;
                if (!string.IsNullOrEmpty(providerTransactionCode))
                {
                    payment.ProviderTransactionCode = providerTransactionCode;
                }
                if (status == "Success")
                {
                    payment.PaidAt = DateTime.UtcNow;
                }
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdatePaymentStatusAtomicAsync(Guid paymentId, string currentStatus, string newStatus, string? providerTransactionCode)
        {
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.Status == currentStatus);
                if (payment == null) return false;
                payment.Status = newStatus;
                payment.ProviderTransactionCode = providerTransactionCode ?? payment.ProviderTransactionCode;
                payment.PaidAt = newStatus == "Success" ? DateTime.UtcNow : payment.PaidAt;
                await _context.SaveChangesAsync();
                return true;
            }

            var rowsAffected = await _context.Payments
                .Where(p => p.PaymentId == paymentId && p.Status == currentStatus)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, newStatus)
                    .SetProperty(p => p.ProviderTransactionCode, p => providerTransactionCode ?? p.ProviderTransactionCode)
                    .SetProperty(p => p.PaidAt, p => newStatus == "Success" ? DateTime.UtcNow : p.PaidAt));
            
            return rowsAffected > 0;
        }

        public async Task<UserCredit?> GetUserCreditAsync(Guid userId)
        {
            return await _context.UserCredits.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task CreateUserCreditAsync(UserCredit userCredit)
        {
            await _context.UserCredits.AddAsync(userCredit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserCreditAsync(UserCredit userCredit)
        {
            userCredit.UpdatedAt = DateTime.UtcNow;
            _context.UserCredits.Update(userCredit);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Payment>> GetPaymentsByUserIdAsync(Guid userId)
        {
            return await _context.Payments
                .Include(p => p.Package)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Payment>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .Include(p => p.Package)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddCreditTransactionAsync(CreditTransaction transaction)
        {
            await _context.CreditTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
