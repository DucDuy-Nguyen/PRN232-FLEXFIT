using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<CreditPackage>> GetActivePackagesAsync();
        Task<CreditPackage?> GetPackageByIdAsync(Guid packageId);
        
        Task CreatePaymentAsync(Payment payment);
        Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
        Task<Payment?> GetPaymentByTransactionCodeAsync(string providerTransactionCode);
        Task UpdatePaymentStatusAsync(Guid paymentId, string status, string? providerTransactionCode);
        
        Task<UserCredit?> GetUserCreditAsync(Guid userId);
        Task CreateUserCreditAsync(UserCredit userCredit);
        Task UpdateUserCreditAsync(UserCredit userCredit);
        
        Task AddCreditTransactionAsync(CreditTransaction transaction);
        Task SaveChangesAsync();
    }
}
