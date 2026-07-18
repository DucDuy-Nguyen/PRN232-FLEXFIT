using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.API.Contracts.Requests.CreditPackages;
using FlexFit.Payment.API.Contracts.Responses.CreditPackages;

namespace FlexFit.Payment.API.Interfaces.Services
{
    public interface ICreditService
    {
        Task<IEnumerable<CreditPackageResponse>> GetAllPackagesAsync();
        Task<CreditPackageResponse?> GetPackageByIdAsync(Guid id);
        Task<Guid> CreatePackageAsync(CreateCreditPackageRequest request);
        Task UpdatePackageAsync(Guid id, UpdateCreditPackageRequest request);
        Task ChangePackageStatusAsync(Guid id, bool isActive);
        Task ChangePackagePopularStatusAsync(Guid id, bool isPopular);
        Task DeletePackageAsync(Guid id);
        Task<UserCreditResponse?> GetUserCreditAsync(Guid userId);
        Task<IEnumerable<CreditTransactionResponse>> GetUserTransactionHistoryAsync(Guid userId);
        Task BuyPackageAsync(Guid packageId, BuyCreditPackageRequest request);
        Task AdminAddCreditToUserAsync(AdminAddCreditRequest request);
    }
}
