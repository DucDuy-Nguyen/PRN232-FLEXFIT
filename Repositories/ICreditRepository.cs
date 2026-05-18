using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface ICreditRepository
    {
        // ---CREDIT PACKAGES---
        Task<IEnumerable<CreditPackage>> GetAllPackagesAsync();
        Task<CreditPackage?> GetPackageByIdAsync(Guid id);
        Task AddPackageAsync(CreditPackage package);
        Task UpdatePackageAsync(CreditPackage package);
        Task DeletePackageAsync(Guid id);
        // --- TRANSACTION & USER CREDIT ---
        Task<UserCredit?> GetUserCreditByUserIdAsync(Guid userId);
        Task AddUserCreditAsync(UserCredit userCredit);
        Task UpdateUserCreditAsync(UserCredit userCredit);
        Task<IEnumerable<CreditTransaction>> GetTransactionsByUserIdAsync(Guid userId);
        Task AddTransactionAsync(CreditTransaction transaction);
    }
}