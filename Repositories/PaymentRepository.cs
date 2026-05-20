using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.Helpers;
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly FlexFitDbContext _db;

        public PaymentRepository(FlexFitDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<CreditPackage>> GetActivePackagesAsync()
        {
            return await _db.CreditPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<CreditPackage?> GetPackageByIdAsync(Guid packageId)
        {
            return await _db.CreditPackages.FindAsync(packageId);
        }

        public async Task CreatePaymentAsync(Payment payment)
        {
            await _db.Payments.AddAsync(payment);
            await _db.SaveChangesAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
        {
            return await _db.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<Payment?> GetPaymentByTransactionCodeAsync(string providerTransactionCode)
        {
            return await _db.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p => p.ProviderTransactionCode == providerTransactionCode);
        }

        public async Task UpdatePaymentStatusAsync(Guid paymentId, string status, string? providerTransactionCode)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = status;
                if (!string.IsNullOrEmpty(providerTransactionCode))
                {
                    payment.ProviderTransactionCode = providerTransactionCode;
                }
                if (status == "Success")
                {
                    payment.PaidAt = DateTimeHelper.GetVietnamTime();
                }
                _db.Payments.Update(payment);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<UserCredit?> GetUserCreditAsync(Guid userId)
        {
            return await _db.UserCredits
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task CreateUserCreditAsync(UserCredit userCredit)
        {
            await _db.UserCredits.AddAsync(userCredit);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateUserCreditAsync(UserCredit userCredit)
        {
            userCredit.UpdatedAt = DateTimeHelper.GetVietnamTime();
            _db.UserCredits.Update(userCredit);
            await _db.SaveChangesAsync();
        }

        public async Task AddCreditTransactionAsync(CreditTransaction transaction)
        {
            await _db.CreditTransactions.AddAsync(transaction);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
