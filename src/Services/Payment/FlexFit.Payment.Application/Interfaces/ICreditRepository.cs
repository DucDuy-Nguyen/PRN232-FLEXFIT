using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.Domain.Entities;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface ICreditRepository
    {
        Task<IEnumerable<CreditPackage>> GetAllPackagesAsync();
        Task<CreditPackage?> GetPackageByIdAsync(Guid id);
        Task AddPackageAsync(CreditPackage package);
        Task UpdatePackageAsync(CreditPackage package);
        Task DeletePackageAsync(Guid id);

        Task<UserCredit?> GetUserCreditByUserIdAsync(Guid userId);
        Task AddUserCreditAsync(UserCredit userCredit);
        Task UpdateUserCreditAsync(UserCredit userCredit);
        Task<IEnumerable<CreditTransaction>> GetTransactionsByUserIdAsync(Guid userId);
        Task AddTransactionAsync(CreditTransaction transaction);
        
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task SaveChangesAsync();
    }
}
