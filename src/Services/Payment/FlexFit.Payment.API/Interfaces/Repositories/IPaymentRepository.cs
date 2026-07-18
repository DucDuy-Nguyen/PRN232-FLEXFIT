using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.API.Domain.Entities;

namespace FlexFit.Payment.API.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<CreditPackage>> GetActivePackagesAsync();
        Task<CreditPackage?> GetPackageByIdAsync(Guid packageId);
        
        Task CreatePaymentAsync(Domain.Entities.Payment payment);
        Task<Domain.Entities.Payment?> GetPaymentByIdAsync(Guid paymentId);
        Task<Domain.Entities.Payment?> GetPaymentByTransactionCodeAsync(string providerTransactionCode);
        Task UpdatePaymentStatusAsync(Guid paymentId, string status, string? providerTransactionCode);
        Task<bool> UpdatePaymentStatusAtomicAsync(Guid paymentId, string currentStatus, string newStatus, string? providerTransactionCode);
        
        Task<UserCredit?> GetUserCreditAsync(Guid userId);
        Task CreateUserCreditAsync(UserCredit userCredit);
        Task UpdateUserCreditAsync(UserCredit userCredit);
        
        Task<IEnumerable<Domain.Entities.Payment>> GetPaymentsByUserIdAsync(Guid userId);
        Task<IEnumerable<Domain.Entities.Payment>> GetAllPaymentsAsync();
        Task AddCreditTransactionAsync(CreditTransaction transaction);
        
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task SaveChangesAsync();
    }
}
