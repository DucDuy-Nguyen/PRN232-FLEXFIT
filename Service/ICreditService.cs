using Flexfit.DTOs.Credit;
using Flexfit.Models;

namespace Flexfit.Services
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
        // --- USER VÀ USER CREDIT (VÍ SỐ DƯ) ---
        // Hàm này cực kỳ quan trọng, giúp Controller lấy thông tin ví (trả về DTO sạch sẽ)
        Task<UserCreditResponse?> GetUserCreditAsync(Guid userId);

        // --- BIẾN ĐỘNG TÀI KHOẢN (TRANSACTION) ---
        Task<IEnumerable<CreditTransactionResponse>> GetUserTransactionHistoryAsync(Guid userId);
        Task BuyPackageAsync(Guid packageId, BuyCreditPackageRequest request);

        // --- TÁC VỤ ĐIỀU CHỈNH CỦA ADMIN ---
        Task AdminAddCreditToUserAsync(AdminAddCreditRequest request);
    }
}