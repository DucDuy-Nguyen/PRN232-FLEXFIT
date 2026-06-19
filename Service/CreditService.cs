using Flexfit.DTOs.Credit;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Services
{
    public class CreditService : ICreditService
    {
        private readonly ICreditRepository _creditRepo;
        private readonly FlexFitDbContext _db; // Inject DbContext phục vụ bảo vệ dữ liệu bằng Transaction

        public CreditService(ICreditRepository creditRepo, FlexFitDbContext db)
        {
            _creditRepo = creditRepo;
            _db = db;
        }

        // ==========================================
        // 1. NHÓM QUẢN LÝ GÓI NẠP (CREDIT PACKAGE)
        // ==========================================

        public async Task<IEnumerable<CreditPackageResponse>> GetAllPackagesAsync()
        {
            var packages = await _creditRepo.GetAllPackagesAsync();
            return packages.Select(p => new CreditPackageResponse
            {
                PackageId = p.PackageId,
                PackageName = p.PackageName,
                CreditAmount = p.CreditAmount,
                Price = p.Price,
                Description = p.Description,
                IsActive = p.IsActive,
                IsPopular = p.IsPopular, // Đảm bảo map thuộc tính này sang Response
                CreatedAt = p.CreatedAt
            });
        }

        public async Task<CreditPackageResponse?> GetPackageByIdAsync(Guid id)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) return null;

            return new CreditPackageResponse
            {
                PackageId = p.PackageId,
                PackageName = p.PackageName,
                CreditAmount = p.CreditAmount,
                Price = p.Price,
                Description = p.Description,
                IsActive = p.IsActive,
                IsPopular = p.IsPopular,
                CreatedAt = p.CreatedAt
            };
        }

        public async Task<Guid> CreatePackageAsync(CreateCreditPackageRequest request)
        {
            var package = new CreditPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = request.PackageName,
                CreditAmount = request.CreditAmount,
                Price = request.Price,
                Description = request.Description,
                IsActive = true,
                IsPopular = false,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _creditRepo.AddPackageAsync(package);
            return package.PackageId;
        }

        public async Task UpdatePackageAsync(Guid id, UpdateCreditPackageRequest request)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            p.PackageName = request.PackageName ?? p.PackageName;
            p.CreditAmount = request.CreditAmount ?? p.CreditAmount;
            p.Price = request.Price ?? p.Price;
            p.Description = request.Description ?? p.Description;

            await _creditRepo.UpdatePackageAsync(p);
        }

        public async Task ChangePackageStatusAsync(Guid id, bool isActive)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            p.IsActive = isActive;
            await _creditRepo.UpdatePackageAsync(p);
        }

        public async Task ChangePackagePopularStatusAsync(Guid id, bool isPopular)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            p.IsPopular = isPopular;
            await _creditRepo.UpdatePackageAsync(p);
        }

        public async Task DeletePackageAsync(Guid id)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            await _creditRepo.DeletePackageAsync(id);
        }

        // ==========================================
        // 2. NHÓM QUẢN LÝ VÍ SỐ DƯ (USER CREDIT)
        // ==========================================

        public async Task<UserCreditResponse?> GetUserCreditAsync(Guid userId)
        {
            var uc = await _creditRepo.GetUserCreditByUserIdAsync(userId);
            if (uc == null) return null;

            return new UserCreditResponse
            {
                UserCreditId = uc.UserCreditId,
                UserId = uc.UserId,
                Balance = uc.Balance,
                TotalEarned = uc.TotalEarned,
                TotalSpent = uc.TotalSpent,
                UpdatedAt = uc.UpdatedAt
            };
        }

        // ==========================================
        // 3. NHÓM BIẾN ĐỘNG TÀI KHOẢN (TRANSACTIONS)
        // ==========================================

        public async Task BuyPackageAsync(Guid packageId, BuyCreditPackageRequest request)
        {
            using var dbTransaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra gói nạp hợp lệ
                var package = await _creditRepo.GetPackageByIdAsync(packageId);
                if (package == null || !package.IsActive)
                    throw new KeyNotFoundException("Gói nạp này không tồn tại hoặc đã ngừng áp dụng.");

                // 2. Lấy ví Credit hiện tại
                var userCredit = await _creditRepo.GetUserCreditByUserIdAsync(request.UserId);

                int balanceBefore = 0;
                int balanceAfter = 0;

                if (userCredit == null)
                {
                    balanceBefore = 0;
                    balanceAfter = package.CreditAmount;

                    userCredit = new UserCredit
                    {
                        UserCreditId = Guid.NewGuid(),
                        UserId = request.UserId,
                        Balance = balanceAfter,
                        TotalEarned = package.CreditAmount, // Tích lũy lần đầu
                        TotalSpent = 0,
                        UpdatedAt = DateTimeHelper.GetVietnamTime()
                    };
                    await _creditRepo.AddUserCreditAsync(userCredit);
                }
                else
                {
                    balanceBefore = userCredit.Balance;
                    balanceAfter = userCredit.Balance + package.CreditAmount;

                    userCredit.Balance = balanceAfter;
                    userCredit.TotalEarned += package.CreditAmount; // Cộng dồn tích lũy
                    userCredit.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    await _creditRepo.UpdateUserCreditAsync(userCredit);
                }

                // 3. Ghi log lịch sử giao dịch
                var transaction = new CreditTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    UserId = request.UserId,
                    Amount = package.CreditAmount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Type = "Deposit",
                    ReferenceId = package.PackageId,
                    ReferenceType = "CreditPackage",
                    Description = $"Nạp thành công {package.CreditAmount} credits từ gói '{package.PackageName}'",
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _creditRepo.AddTransactionAsync(transaction);

                // Commit lưu toàn bộ thay đổi an toàn xuống DB
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task AdminAddCreditToUserAsync(AdminAddCreditRequest request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Số lượng credit cộng thêm phải lớn hơn 0.");

            using var dbTransaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var userCredit = await _creditRepo.GetUserCreditByUserIdAsync(request.UserId);

                int balanceBefore = 0;
                int balanceAfter = 0;

                if (userCredit == null)
                {
                    balanceBefore = 0;
                    balanceAfter = request.Amount;

                    userCredit = new UserCredit
                    {
                        UserCreditId = Guid.NewGuid(),
                        UserId = request.UserId,
                        Balance = balanceAfter,
                        TotalEarned = request.Amount,
                        TotalSpent = 0,
                        UpdatedAt = DateTimeHelper.GetVietnamTime()
                    };
                    await _creditRepo.AddUserCreditAsync(userCredit);
                }
                else
                {
                    balanceBefore = userCredit.Balance;
                    balanceAfter = userCredit.Balance + request.Amount;

                    userCredit.Balance = balanceAfter;
                    userCredit.TotalEarned += request.Amount;
                    userCredit.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    await _creditRepo.UpdateUserCreditAsync(userCredit);
                }

                var transaction = new CreditTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    UserId = request.UserId,
                    Amount = request.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Type = "AdminAdjustment",
                    ReferenceId = null,
                    ReferenceType = "Admin",
                    Description = string.IsNullOrWhiteSpace(request.Description)
                        ? $"Admin cộng {request.Amount} credits vào tài khoản."
                        : $"[Admin điều chỉnh] {request.Description}",
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _creditRepo.AddTransactionAsync(transaction);

                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<CreditTransactionResponse>> GetUserTransactionHistoryAsync(Guid userId)
        {
            var transactions = await _creditRepo.GetTransactionsByUserIdAsync(userId);
            return transactions.Select(t => new CreditTransactionResponse
            {
                TransactionId = t.TransactionId,
                UserId = t.UserId,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Type = t.Type,
                ReferenceId = t.ReferenceId,
                ReferenceType = t.ReferenceType,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            });
        }
    }
}