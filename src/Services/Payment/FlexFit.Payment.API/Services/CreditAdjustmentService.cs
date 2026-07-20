using System;
using System.Linq;
using System.Threading.Tasks;
using FlexFit.Payment.API.Interfaces.Services;
using FlexFit.Payment.API.Interfaces.Repositories;
using FlexFit.Payment.API.Domain.Entities;
using FlexFit.Payment.API.Contracts.Events;

namespace FlexFit.Payment.API.Services
{
    public class CreditAdjustmentService : ICreditAdjustmentService
    {
        private readonly ICreditRepository _creditRepo;
        private readonly IDistributedLockService _lockService;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ICacheService _cacheService;
        private readonly IIdempotencyService _idempotencyService;
        private readonly IProcessedMessageRepository _processedRepo;

        public CreditAdjustmentService(
            ICreditRepository creditRepo,
            IDistributedLockService lockService,
            IOutboxRepository outboxRepository,
            ICacheService cacheService,
            IIdempotencyService idempotencyService,
            IProcessedMessageRepository processedRepo)
        {
            _creditRepo = creditRepo;
            _lockService = lockService;
            _outboxRepository = outboxRepository;
            _cacheService = cacheService;
            _idempotencyService = idempotencyService;
            _processedRepo = processedRepo;
        }

        public async Task DeductCreditAsync(Guid bookingId, Guid userId, int creditCost, string referenceType, string description)
        {
            if (creditCost <= 0)
            {
                throw new ArgumentException("Số lượng credit khấu trừ phải lớn hơn 0.");
            }

            var lockToken = Guid.NewGuid().ToString();
            var lockKey = $"lock:user:{userId}:wallet";

            if (!await _lockService.AcquireLockAsync(lockKey, lockToken, TimeSpan.FromSeconds(15)))
            {
                throw new Exception("Hệ thống ví bận, vui lòng thử lại.");
            }

            try
            {
                if (await _processedRepo.HasBeenProcessedAsync(bookingId))
                {
                    return; // Already processed
                }

                // SQL Idempotency: check if credit transaction already exists for this booking ID reference
                var transactions = await _creditRepo.GetTransactionsByUserIdAsync(userId);
                var existing = transactions.FirstOrDefault(t => t.ReferenceId == bookingId && t.Type == "Deduction");
                if (existing != null)
                {
                    return; // Already processed
                }

                var redisIdempotencyKey = $"idempotency:credit:deduct:{bookingId}";
                if (!await _idempotencyService.IsIdempotentAsync(redisIdempotencyKey, TimeSpan.FromDays(1)))
                {
                    return; // Already processed in Redis
                }

                await _creditRepo.BeginTransactionAsync();

                var wallet = await _creditRepo.GetUserCreditByUserIdAsync(userId);
                if (wallet == null || wallet.Balance < creditCost)
                {
                    // Insufficient balance, queue failure event
                    var failEvent = new CreditDeductionFailed
                    {
                        BookingId = bookingId,
                        UserId = userId,
                        CreditCost = creditCost,
                        Reason = "Tài khoản không đủ số dư tín dụng."
                    };
                    await _outboxRepository.QueueEventAsync("CreditDeductionFailed", failEvent);
                    await _creditRepo.SaveChangesAsync();
                    await _creditRepo.CommitTransactionAsync();
                    return;
                }

                int balanceBefore = wallet.Balance;
                wallet.Balance -= creditCost;
                wallet.TotalSpent += creditCost;
                wallet.UpdatedAt = DateTime.UtcNow;

                await _creditRepo.UpdateUserCreditAsync(wallet);

                var txn = new CreditTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    UserId = userId,
                    Amount = -creditCost,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = "Deduction",
                    ReferenceId = bookingId,
                    ReferenceType = referenceType,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                };

                await _creditRepo.AddTransactionAsync(txn);

                var creditDeductedEvent = new CreditDeductionSucceeded
                {
                    BookingId = bookingId,
                    UserId = userId,
                    CreditCost = creditCost,
                    NewBalance = wallet.Balance
                };
                await _outboxRepository.QueueEventAsync("CreditDeductionSucceeded", creditDeductedEvent);
                await _processedRepo.MarkAsProcessedAsync(bookingId);
                await _creditRepo.SaveChangesAsync();
                await _creditRepo.CommitTransactionAsync();

                // Invalidate cache
                await _cacheService.RemoveAsync($"payment:user:{userId}:balance");
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

        public async Task RefundCreditAsync(Guid bookingId, Guid userId, int refundCredit, string referenceType, string description)
        {
            if (refundCredit <= 0)
            {
                return; // Nothing to refund
            }

            var lockToken = Guid.NewGuid().ToString();
            var lockKey = $"lock:user:{userId}:wallet";

            if (!await _lockService.AcquireLockAsync(lockKey, lockToken, TimeSpan.FromSeconds(15)))
            {
                throw new Exception("Hệ thống ví bận, vui lòng thử lại.");
            }

            try
            {
                if (await _processedRepo.HasBeenProcessedAsync(bookingId))
                {
                    return; // Already processed
                }

                // SQL Idempotency: check if credit transaction already exists for this booking ID reference
                var transactions = await _creditRepo.GetTransactionsByUserIdAsync(userId);
                var existing = transactions.FirstOrDefault(t => t.ReferenceId == bookingId && t.Type == "Refund");
                if (existing != null)
                {
                    return; // Already processed
                }

                var redisIdempotencyKey = $"idempotency:credit:refund:{bookingId}";
                if (!await _idempotencyService.IsIdempotentAsync(redisIdempotencyKey, TimeSpan.FromDays(1)))
                {
                    return; // Already processed in Redis
                }

                await _creditRepo.BeginTransactionAsync();

                var wallet = await _creditRepo.GetUserCreditByUserIdAsync(userId);
                if (wallet == null)
                {
                    wallet = new UserCredit
                    {
                        UserCreditId = Guid.NewGuid(),
                        UserId = userId,
                        Balance = refundCredit,
                        TotalEarned = refundCredit,
                        TotalSpent = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _creditRepo.AddUserCreditAsync(wallet);
                }
                else
                {
                    int balanceBefore = wallet.Balance;
                    wallet.Balance += refundCredit;
                    wallet.TotalSpent = Math.Max(0, wallet.TotalSpent - refundCredit);
                    wallet.UpdatedAt = DateTime.UtcNow;

                    await _creditRepo.UpdateUserCreditAsync(wallet);

                    var txn = new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = refundCredit,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = wallet.Balance,
                        Type = "Refund",
                        ReferenceId = bookingId,
                        ReferenceType = referenceType,
                        Description = description,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _creditRepo.AddTransactionAsync(txn);
                }

                var creditRefundedEvent = new CreditRefundSucceeded
                {
                    BookingId = bookingId,
                    UserId = userId,
                    RefundCredit = refundCredit,
                    NewBalance = wallet.Balance
                };
                await _outboxRepository.QueueEventAsync("CreditRefundSucceeded", creditRefundedEvent);
                await _processedRepo.MarkAsProcessedAsync(bookingId);
                await _creditRepo.SaveChangesAsync();
                await _creditRepo.CommitTransactionAsync();

                // Invalidate cache
                await _cacheService.RemoveAsync($"payment:user:{userId}:balance");
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
    }
}
