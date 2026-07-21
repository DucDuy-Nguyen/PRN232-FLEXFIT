using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlexFit.Payment.API.DTOs.Requests;
using FlexFit.Payment.API.DTOs.Responses;
using FlexFit.Payment.API.Services.Interfaces;
using FlexFit.Payment.API.Infrastructure.Redis.Interfaces;
using FlexFit.Payment.API.Gateways.Interfaces;
using FlexFit.Payment.API.DTOs.Events;
using FlexFit.Payment.API.Repositories.Interfaces;
using FlexFit.Payment.API.Domain.Entities;

namespace FlexFit.Payment.API.Services.Implementations
{
    public class CreditService : ICreditService
    {
        private readonly ICreditRepository _creditRepo;
        private readonly IDistributedLockService _lockService;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ICacheService _cacheService;
        private readonly IIdempotencyService _idempotencyService;

        public CreditService(
            ICreditRepository creditRepo,
            IDistributedLockService lockService,
            IOutboxRepository outboxRepository,
            ICacheService cacheService,
            IIdempotencyService idempotencyService)
        {
            _creditRepo = creditRepo;
            _lockService = lockService;
            _outboxRepository = outboxRepository;
            _cacheService = cacheService;
            _idempotencyService = idempotencyService;
        }

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
                IsPopular = p.IsPopular,
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
                CreatedAt = DateTime.UtcNow
            };

            await _creditRepo.AddPackageAsync(package);
            await _cacheService.RemoveAsync("payment:packages:active");
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
            await _cacheService.RemoveAsync("payment:packages:active");
        }

        public async Task ChangePackageStatusAsync(Guid id, bool isActive)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            p.IsActive = isActive;
            await _creditRepo.UpdatePackageAsync(p);
            await _cacheService.RemoveAsync("payment:packages:active");
        }

        public async Task ChangePackagePopularStatusAsync(Guid id, bool isPopular)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            p.IsPopular = isPopular;
            await _creditRepo.UpdatePackageAsync(p);
            await _cacheService.RemoveAsync("payment:packages:active");
        }

        public async Task DeletePackageAsync(Guid id)
        {
            var p = await _creditRepo.GetPackageByIdAsync(id);
            if (p == null) throw new KeyNotFoundException("Không tìm thấy gói nạp yêu cầu.");

            await _creditRepo.DeletePackageAsync(id);
            await _cacheService.RemoveAsync("payment:packages:active");
        }

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

        public async Task BuyPackageAsync(Guid packageId, BuyCreditPackageRequest request)
        {
            var lockToken = Guid.NewGuid().ToString();
            var lockKey = $"lock:user:{request.UserId}:wallet";

            if (!await _lockService.AcquireLockAsync(lockKey, lockToken, TimeSpan.FromSeconds(15)))
            {
                throw new Exception("Hệ thống ví đang bận, vui lòng thử lại.");
            }

            try
            {
                var package = await _creditRepo.GetPackageByIdAsync(packageId);
                if (package == null || !package.IsActive)
                    throw new KeyNotFoundException("Gói nạp này không tồn tại hoặc đã ngừng áp dụng.");

                await _creditRepo.BeginTransactionAsync();

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
                        TotalEarned = package.CreditAmount,
                        TotalSpent = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _creditRepo.AddUserCreditAsync(userCredit);
                }
                else
                {
                    balanceBefore = userCredit.Balance;
                    balanceAfter = userCredit.Balance + package.CreditAmount;

                    userCredit.Balance = balanceAfter;
                    userCredit.TotalEarned += package.CreditAmount;
                    userCredit.UpdatedAt = DateTime.UtcNow;
                    await _creditRepo.UpdateUserCreditAsync(userCredit);
                }

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
                    Description = $"Nạp thành công {package.CreditAmount} credits từ gói '{package.PackageName}' (Mock Buy)",
                    CreatedAt = DateTime.UtcNow
                };

                await _creditRepo.AddTransactionAsync(transaction);

                // Publish Outbox Event
                var creditAdjustedEvent = new CreditAdjusted
                {
                    UserId = request.UserId,
                    Amount = package.CreditAmount,
                    NewBalance = balanceAfter,
                    Description = transaction.Description
                };
                await _outboxRepository.QueueEventAsync("CreditAdjusted", creditAdjustedEvent);

                await _creditRepo.SaveChangesAsync();
                await _creditRepo.CommitTransactionAsync();

                // Invalidate Cache
                await _cacheService.RemoveAsync($"payment:user:{request.UserId}:balance");
                await _cacheService.RemoveAsync("payment:admin:revenue_summary");
            }
            catch (Exception)
            {
                await _creditRepo.RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _lockService.ReleaseLockAsync(lockKey, lockToken);
            }
        }

        public async Task AdminAddCreditToUserAsync(AdminAddCreditRequest request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Số lượng credit cộng thêm phải lớn hơn 0.");

            var lockToken = Guid.NewGuid().ToString();
            var lockKey = $"lock:user:{request.UserId}:wallet";

            if (!await _lockService.AcquireLockAsync(lockKey, lockToken, TimeSpan.FromSeconds(15)))
            {
                throw new Exception("Hệ thống ví đang bận, vui lòng thử lại.");
            }

            try
            {
                await _creditRepo.BeginTransactionAsync();

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
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _creditRepo.AddUserCreditAsync(userCredit);
                }
                else
                {
                    balanceBefore = userCredit.Balance;
                    balanceAfter = userCredit.Balance + request.Amount;

                    userCredit.Balance = balanceAfter;
                    userCredit.TotalEarned += request.Amount;
                    userCredit.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow
                };

                await _creditRepo.AddTransactionAsync(transaction);

                // Publish Outbox Event
                var creditAdjustedEvent = new CreditAdjusted
                {
                    UserId = request.UserId,
                    Amount = request.Amount,
                    NewBalance = balanceAfter,
                    Description = transaction.Description
                };
                await _outboxRepository.QueueEventAsync("CreditAdjusted", creditAdjustedEvent);

                await _creditRepo.SaveChangesAsync();
                await _creditRepo.CommitTransactionAsync();

                // Invalidate Cache
                await _cacheService.RemoveAsync($"payment:user:{request.UserId}:balance");
                await _cacheService.RemoveAsync("payment:admin:revenue_summary");
            }
            catch (Exception)
            {
                await _creditRepo.RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _lockService.ReleaseLockAsync(lockKey, lockToken);
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


