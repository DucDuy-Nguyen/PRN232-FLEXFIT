using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.Repository.Entities;

namespace FlexFit.Payment.Repository.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<CreditPackage>> GetActivePackagesAsync();
        Task<CreditPackage?> GetPackageByIdAsync(Guid packageId);
        
        Task CreatePaymentAsync(Entities.Payment payment);
        Task<Entities.Payment?> GetPaymentByIdAsync(Guid paymentId);
        Task<Entities.Payment?> GetPaymentByTransactionCodeAsync(string providerTransactionCode);
        Task UpdatePaymentStatusAsync(Guid paymentId, string status, string? providerTransactionCode);
        Task<bool> UpdatePaymentStatusAtomicAsync(Guid paymentId, string currentStatus, string newStatus, string? providerTransactionCode);
        
        Task<UserCredit?> GetUserCreditAsync(Guid userId);
        Task CreateUserCreditAsync(UserCredit userCredit);
        Task UpdateUserCreditAsync(UserCredit userCredit);
        
        Task<IEnumerable<Entities.Payment>> GetPaymentsByUserIdAsync(Guid userId);
        Task<IEnumerable<Entities.Payment>> GetAllPaymentsAsync();
        Task AddCreditTransactionAsync(CreditTransaction transaction);
        
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task SaveChangesAsync();
    }
}
