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
using FlexFit.Payment.API.Configurations;
using FlexFit.Payment.API.Repositories.Interfaces;
using FlexFit.Payment.API.Domain.Entities;

namespace FlexFit.Payment.API.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPayOSPaymentGateway _payOSGateway;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IDistributedLockService _lockService;
        private readonly IIdempotencyService _idempotencyService;
        private readonly ICacheService _cacheService;
        private readonly PayOSOptions _payOSOptions;
        private readonly PaymentOptions _paymentOptions;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IPayOSPaymentGateway payOSGateway,
            IOutboxRepository outboxRepository,
            IDistributedLockService lockService,
            IIdempotencyService idempotencyService,
            ICacheService cacheService,
            Microsoft.Extensions.Options.IOptions<PayOSOptions> payOSOptions,
            Microsoft.Extensions.Options.IOptions<PaymentOptions> paymentOptions)
        {
            _paymentRepository = paymentRepository;
            _payOSGateway = payOSGateway;
            _outboxRepository = outboxRepository;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
            _cacheService = cacheService;
            _payOSOptions = payOSOptions.Value;
            _paymentOptions = paymentOptions.Value;
        }

        // Backward compatibility constructor for tests
        public PaymentService(
            IPaymentRepository paymentRepository,
            IPayOSPaymentGateway payOSGateway,
            IOutboxRepository outboxRepository,
            IDistributedLockService lockService,
            IIdempotencyService idempotencyService,
            ICacheService cacheService)
            : this(
                paymentRepository,
                payOSGateway,
                outboxRepository,
                lockService,
                idempotencyService,
                cacheService,
                Microsoft.Extensions.Options.Options.Create(new PayOSOptions { ClientId = "mock", ApiKey = "mock", ChecksumKey = "mock" }),
                Microsoft.Extensions.Options.Options.Create(new PaymentOptions { UseMockPayment = true }))
        {
        }

        public async Task<IEnumerable<CreditPackageResponse>> GetPackagesAsync()
        {
            var cacheKey = "payment:packages:active";
            var cached = await _cacheService.GetAsync<List<CreditPackageResponse>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var packages = await _paymentRepository.GetActivePackagesAsync();
            var response = packages.Select(p => new CreditPackageResponse
            {
                PackageId = p.PackageId,
                PackageName = p.PackageName,
                CreditAmount = p.CreditAmount,
                BonusCredit = p.BonusCredit,
                Price = p.Price,
                Description = p.Description,
                IsPopular = p.IsPopular,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            }).ToList();

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1));
            return response;
        }

        public async Task<PaymentResponse> CreatePaymentUrlAsync(Guid userId, CreatePaymentRequest request)
        {
            var package = await _paymentRepository.GetPackageByIdAsync(request.PackageId);
            if (package == null || !package.IsActive)
            {
                throw new ArgumentException("Gói tín dụng không tồn tại hoặc đã bị khóa.");
            }

            string paymentMethod = request.PaymentMethod.ToUpper();
            if (!_paymentOptions.UseMockPayment)
            {
                paymentMethod = "PAYOS";
                if (string.IsNullOrWhiteSpace(_payOSOptions.ClientId) ||
                    string.IsNullOrWhiteSpace(_payOSOptions.ApiKey) ||
                    string.IsNullOrWhiteSpace(_payOSOptions.ChecksumKey))
                {
                    throw new InvalidOperationException("Cấu hình PayOS bị thiếu khi UseMockPayment là false.");
                }
            }

            string? providerTransactionCode = null;
            long orderCode = 0;
            if (paymentMethod == "PAYOS")
            {
                orderCode = DateTime.UtcNow.Ticks / 10000;
                providerTransactionCode = orderCode.ToString();
            }

            var paymentId = Guid.NewGuid();
            var payment = new FlexFit.Payment.API.Domain.Entities.Payment
            {
                PaymentId = paymentId,
                UserId = userId,
                PackageId = request.PackageId,
                Amount = package.Price,
                PaymentMethod = paymentMethod,
                Status = "Pending",
                ProviderTransactionCode = providerTransactionCode,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.CreatePaymentAsync(payment);

            string paymentUrl = "";
            switch (paymentMethod)
            {
                case "PAYOS":
                    var descriptionText = $"Nap {package.CreditAmount} credit";
                    try
                    {
                        var cancelUrl = "https://www.flexfit.io.vn/payment/cancel";
                        var returnUrl = "https://www.flexfit.io.vn/payment/success";
                        var linkResult = await _payOSGateway.CreatePaymentLinkAsync(
                            orderCode, 
                            (int)package.Price, 
                            descriptionText, 
                            cancelUrl, 
                            returnUrl);
                        paymentUrl = linkResult.CheckoutUrl;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Không thể tạo link thanh toán PayOS: {ex.Message}");
                    }
                    break;
                case "VNPAY":
                case "MOMO":
                case "MOCK":
                default:
                    if (!_paymentOptions.UseMockPayment)
                    {
                        throw new InvalidOperationException("Không hỗ trợ phương thức thanh toán Mock khi UseMockPayment là false.");
                    }
                    paymentUrl = $"/api/payment/mock-checkout?paymentId={paymentId}&amount={package.Price}";
                    break;
            }

            return new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                UserId = payment.UserId,
                PackageId = payment.PackageId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentUrl = paymentUrl,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt
            };
        }

        public async Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackRequest callbackData)
        {
            var lockToken = Guid.NewGuid().ToString();
            var lockKey = $"lock:payment:{callbackData.PaymentId}";
            var walletLockKey = "";
            var walletLockToken = Guid.NewGuid().ToString();
            bool paymentLockAcquired = false;
            bool walletLockAcquired = false;

            try
            {
                try {
                    paymentLockAcquired = await _lockService.AcquireLockAsync(lockKey, lockToken, TimeSpan.FromSeconds(10));
                } catch (Exception ex) {
                    Console.WriteLine($"[Redis Error] AcquireLockAsync payment: {ex.Message}");
                }

                var payment = await _paymentRepository.GetPaymentByIdAsync(callbackData.PaymentId);
                if (payment == null)
                {
                    return false;
                }

                if (payment.Status != "Pending")
                {
                    return payment.Status == "Success";
                }

                walletLockKey = $"lock:user:{payment.UserId}:wallet";
                try {
                    walletLockAcquired = await _lockService.AcquireLockAsync(walletLockKey, walletLockToken, TimeSpan.FromSeconds(15));
                } catch (Exception ex) {
                    Console.WriteLine($"[Redis Error] AcquireLockAsync wallet: {ex.Message}");
                }

                try
                {
                    try {
                        var idempotencyKey = $"idempotency:payment:{callbackData.PaymentId}";
                        if (!await _idempotencyService.IsIdempotentAsync(idempotencyKey, TimeSpan.FromDays(1)))
                        {
                            return true; // Already processed
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"[Redis Error] IsIdempotentAsync: {ex.Message}");
                    }

                    await _paymentRepository.BeginTransactionAsync();

                    if (callbackData.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
                    {
                        var providerTxCode = callbackData.ProviderTransactionCode ?? $"MOCK_TXN_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                        bool updated = await _paymentRepository.UpdatePaymentStatusAtomicAsync(
                            payment.PaymentId,
                            "Pending",
                            "Success",
                            providerTxCode
                        );

                        if (!updated)
                        {
                            await _paymentRepository.RollbackTransactionAsync();
                            return true; // Another process already updated it
                        }

                        var package = payment.Package;
                        int totalCreditToAdd = package.CreditAmount + package.BonusCredit;

                        var userCredit = await _paymentRepository.GetUserCreditAsync(payment.UserId);
                        int balanceBefore = 0;

                        if (userCredit == null)
                        {
                            userCredit = new UserCredit
                            {
                                UserCreditId = Guid.NewGuid(),
                                UserId = payment.UserId,
                                Balance = totalCreditToAdd,
                                TotalEarned = totalCreditToAdd,
                                TotalSpent = 0,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _paymentRepository.CreateUserCreditAsync(userCredit);
                        }
                        else
                        {
                            balanceBefore = userCredit.Balance;
                            userCredit.Balance += totalCreditToAdd;
                            userCredit.TotalEarned += totalCreditToAdd;
                            await _paymentRepository.UpdateUserCreditAsync(userCredit);
                        }

                        var transaction = new CreditTransaction
                        {
                            TransactionId = Guid.NewGuid(),
                            UserId = payment.UserId,
                            Amount = totalCreditToAdd,
                            BalanceBefore = balanceBefore,
                            BalanceAfter = balanceBefore + totalCreditToAdd,
                            Type = "Deposit",
                            ReferenceId = payment.PaymentId,
                            ReferenceType = "Payment",
                            Description = $"Nạp tín dụng từ gói {package.PackageName}",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _paymentRepository.AddCreditTransactionAsync(transaction);

                        // Queue Event to SQL Outbox
                        var paymentCompletedEvent = new PaymentCompleted
                        {
                            PaymentId = payment.PaymentId,
                            UserId = payment.UserId,
                            PackageId = payment.PackageId,
                            PackageName = package.PackageName,
                            AmountPaid = payment.Amount,
                            CreditsAdded = totalCreditToAdd,
                            NewBalance = userCredit.Balance
                        };

                        await _outboxRepository.QueueEventAsync("PaymentCompleted", paymentCompletedEvent);
                        await _paymentRepository.SaveChangesAsync();
                        await _paymentRepository.CommitTransactionAsync();

                        // Invalidate cache
                        try {
                            await _cacheService.RemoveAsync($"payment:user:{payment.UserId}:balance");
                            await _cacheService.RemoveAsync("payment:admin:revenue_summary");
                        } catch (Exception ex) {
                            Console.WriteLine($"[Redis Error] RemoveAsync: {ex.Message}");
                        }

                        return true;
                    }
                    else
                    {
                        bool updated = await _paymentRepository.UpdatePaymentStatusAtomicAsync(
                            payment.PaymentId,
                            "Pending",
                            "Failed",
                            null
                        );

                        if (!updated)
                        {
                            await _paymentRepository.RollbackTransactionAsync();
                            return false;
                        }

                        var paymentFailedEvent = new PaymentFailed
                        {
                            PaymentId = payment.PaymentId,
                            UserId = payment.UserId,
                            Reason = callbackData.Message ?? "Giao dịch bị huỷ hoặc thất bại."
                        };

                        await _outboxRepository.QueueEventAsync("PaymentFailed", paymentFailedEvent);
                        await _paymentRepository.SaveChangesAsync();
                        await _paymentRepository.CommitTransactionAsync();

                        return false;
                    }
                }
                catch (Exception)
                {
                    await _paymentRepository.RollbackTransactionAsync();
                    throw;
                }
                finally
                {
                    if (walletLockAcquired) {
                        try {
                            await _lockService.ReleaseLockAsync(walletLockKey, walletLockToken);
                        } catch (Exception ex) {
                            Console.WriteLine($"[Redis Error] ReleaseLockAsync wallet: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                if (paymentLockAcquired) {
                    try {
                        await _lockService.ReleaseLockAsync(lockKey, lockToken);
                    } catch (Exception ex) {
                        Console.WriteLine($"[Redis Error] ReleaseLockAsync payment: {ex.Message}");
                    }
                }
            }
        }

        public async Task<bool> ProcessPayOSWebhookAsync(object webhookBody)
        {
            var verifiedData = await _payOSGateway.VerifyWebhookSignatureAsync(webhookBody);
            if (verifiedData == null)
            {
                throw new Exception("Xác thực chữ ký PayOS thất bại hoặc dữ liệu không hợp lệ.");
            }

            if (verifiedData.OrderCode == 0)
            {
                return true; // Verification webhook test
            }

            var payment = await _paymentRepository.GetPaymentByTransactionCodeAsync(verifiedData.OrderCode.ToString());
            if (payment == null)
            {
                return false;
            }

            if (payment.Status != "Pending")
            {
                return true; // Already processed
            }

            var callbackRequest = new PaymentCallbackRequest
            {
                PaymentId = payment.PaymentId,
                Status = "Success",
                ProviderTransactionCode = verifiedData.OrderCode.ToString(),
                Message = "Xử lý thành công qua PayOS Webhook"
            };

            return await ProcessPaymentCallbackAsync(callbackRequest);
        }

        public async Task<UserCredit?> GetUserCreditAsync(Guid userId)
        {
            var cacheKey = $"payment:user:{userId}:balance";
            var cachedBalance = await _cacheService.GetAsync<int?>(cacheKey);
            if (cachedBalance.HasValue)
            {
                return new UserCredit
                {
                    UserId = userId,
                    Balance = cachedBalance.Value,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            var credit = await _paymentRepository.GetUserCreditAsync(userId);
            if (credit != null)
            {
                await _cacheService.SetAsync(cacheKey, (int?)credit.Balance, TimeSpan.FromMinutes(30));
            }
            return credit;
        }

        public async Task<IEnumerable<PaymentHistoryDto>> GetUserPaymentHistoryAsync(Guid userId)
        {
            var payments = await _paymentRepository.GetPaymentsByUserIdAsync(userId);
            return payments.Select(p => new PaymentHistoryDto
            {
                PaymentId = p.PaymentId,
                UserId = p.UserId,
                PackageId = p.PackageId,
                PackageName = p.Package?.PackageName,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ProviderTransactionCode = p.ProviderTransactionCode,
                Status = p.Status,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            });
        }

        public async Task<PaymentHistoryDto?> GetPaymentStatusAsync(Guid paymentId, Guid userId)
        {
            var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId);
            if (payment == null || payment.UserId != userId)
            {
                return null;
            }

            return new PaymentHistoryDto
            {
                PaymentId = payment.PaymentId,
                UserId = payment.UserId,
                PackageId = payment.PackageId,
                PackageName = payment.Package?.PackageName,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                ProviderTransactionCode = payment.ProviderTransactionCode,
                Status = payment.Status,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt
            };
        }

        public async Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllPaymentsAsync();
            return payments.Select(p => new PaymentHistoryDto
            {
                PaymentId = p.PaymentId,
                UserId = p.UserId,
                PackageId = p.PackageId,
                PackageName = p.Package?.PackageName,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ProviderTransactionCode = p.ProviderTransactionCode,
                Status = p.Status,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            });
        }
    }
}



