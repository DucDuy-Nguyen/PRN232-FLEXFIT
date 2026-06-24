using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.DTOs.Payment;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace Flexfit.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly PayOSClient _payOSClient;
        private readonly ISystemLogService _systemLogService;
        private readonly INotificationService _notificationService; // <-- THÊM ĐỊNH NGHĨA SERVICE


        public PaymentService(IPaymentRepository paymentRepository, PayOSClient payOSClient, ISystemLogService systemLogService, INotificationService notificationService)
        {
            _paymentRepository = paymentRepository;
            _payOSClient = payOSClient;
            _systemLogService = systemLogService;
            _notificationService = notificationService; 
        }


        public async Task<IEnumerable<CreditPackageResponse>> GetPackagesAsync()
        {
            var packages = await _paymentRepository.GetActivePackagesAsync();
            return packages.Select(p => new CreditPackageResponse
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
            });
        }

        public async Task<PaymentResponse> CreatePaymentUrlAsync(Guid userId, CreatePaymentRequest request)
        {
            var package = await _paymentRepository.GetPackageByIdAsync(request.PackageId);
            if (package == null || !package.IsActive)
            {
                throw new ArgumentException("Gói tín dụng không tồn tại hoặc đã bị khóa.");
            }

            string? providerTransactionCode = null;
            long orderCode = 0;
            if (request.PaymentMethod.ToUpper() == "PAYOS")
            {
                orderCode = DateTime.UtcNow.Ticks / 10000;
                providerTransactionCode = orderCode.ToString();
            }

            var paymentId = Guid.NewGuid();
            var payment = new Payment
            {
                PaymentId = paymentId,
                UserId = userId,
                PackageId = request.PackageId,
                Amount = package.Price,
                PaymentMethod = request.PaymentMethod.ToUpper(),
                Status = "Pending",
                ProviderTransactionCode = providerTransactionCode,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _paymentRepository.CreatePaymentAsync(payment);

            string paymentUrl = "";
            switch (request.PaymentMethod.ToUpper())
            {
                case "PAYOS":
                    var descriptionText = $"Nap {package.CreditAmount} credit";
                    var paymentRequest = new CreatePaymentLinkRequest
                    {
                        OrderCode = orderCode,
                        Amount = (int)package.Price,
                        Description = descriptionText.Substring(0, Math.Min(25, descriptionText.Length)),
                        CancelUrl = "https://www.flexfit.io.vn/payment/cancel",
                        ReturnUrl = "https://www.flexfit.io.vn/payment/success"
                    };

                    try
                    {
                        var createPaymentResult = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);
                        paymentUrl = createPaymentResult.CheckoutUrl;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Không thể tạo link thanh toán PayOS: {ex.Message}");
                    }
                    break;
                case "VNPAY":
                    paymentUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?mock=true&paymentId={paymentId}&amount={package.Price}";
                    break;
                case "MOMO":
                    paymentUrl = $"https://test-payment.momo.vn/v2/gateway/api/create?mock=true&paymentId={paymentId}&amount={package.Price}";
                    break;
                case "MOCK":
                default:
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
            var payment = await _paymentRepository.GetPaymentByIdAsync(callbackData.PaymentId);
            if (payment == null)
            {
                return false;
            }

            if (payment.Status != "Pending")
            {
                return payment.Status == "Success";
            }

            if (callbackData.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
            {
                await _paymentRepository.UpdatePaymentStatusAsync(
                    payment.PaymentId,
                    "Success",
                    callbackData.ProviderTransactionCode ?? $"MOCK_TXN_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
                );

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
                        UpdatedAt = DateTimeHelper.GetVietnamTime()
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
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _paymentRepository.AddCreditTransactionAsync(transaction);
                await _systemLogService.LogActionAsync(payment.UserId, "DEPOSIT_SUCCESS", $"Nạp tín dụng thành công từ gói {package.PackageName}. Số tiền: {payment.Amount:N0} VNĐ. Nhận được: {totalCreditToAdd} Credits.", null);


                // ==========================================
                // TÍCH HỢP: GỬI THÔNG BÁO CHO CALLBACK (MOCK)
                // ==========================================
                try
                {
                    await _notificationService.SendAsync(
                        payment.UserId,
                        "Nạp ví thành công 🎉",
                        $"Tài khoản của bạn đã được cộng +{totalCreditToAdd} tín dụng từ gói {package.PackageName}.",
                        NotificationTypes.PaymentSuccess // Hãy đổi thành Notification.PaymentSuccess nếu dùng theo cách 2
                    );
                    // Broadcast credit balance update
                    var updatedCredit = await _paymentRepository.GetUserCreditAsync(payment.UserId);
                    if (updatedCredit != null)
                    {
                        await _notificationService.BroadcastCreditUpdateAsync(payment.UserId, updatedCredit.Balance);
                    }
                }
                catch (Exception)
                {
                    // Log lỗi nếu cần, tránh làm crash luồng xử lý chính của giao dịch
                }

                return true;
            }
            else
            {
                await _paymentRepository.UpdatePaymentStatusAsync(payment.PaymentId, "Failed", null);
                return false;
            }
        }

        public async Task<bool> ProcessPayOSWebhookAsync(Webhook webhookBody)
        {
            var verifiedData = await _payOSClient.Webhooks.VerifyAsync(webhookBody);
            if (verifiedData == null)
            {
                throw new Exception("Xác thực chữ ký PayOS thất bại hoặc dữ liệu không hợp lệ.");
            }

            // PayOS gửi webhook test xác nhận với orderCode = 0 khi cấu hình URL
            if (verifiedData.OrderCode == 0)
            {
                return true; // Trả OK để PayOS xác nhận webhook URL hợp lệ
            }

            var payment = await _paymentRepository.GetPaymentByTransactionCodeAsync(verifiedData.OrderCode.ToString());
            if (payment == null)
            {
                return false;
            }

            if (payment.Status != "Pending")
            {
                return true;
            }

            await _paymentRepository.UpdatePaymentStatusAsync(
                payment.PaymentId,
                "Success",
                verifiedData.OrderCode.ToString()
            );

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
                    UpdatedAt = DateTimeHelper.GetVietnamTime()
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
                Description = $"Nạp tín dụng từ gói {package.PackageName} qua PayOS (VietQR)",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _paymentRepository.AddCreditTransactionAsync(transaction);
            await _systemLogService.LogActionAsync(payment.UserId, "DEPOSIT_SUCCESS", $"Nạp tín dụng thành công từ gói {package.PackageName} qua PayOS (VietQR). Số tiền: {payment.Amount:N0} VNĐ. Nhận được: {totalCreditToAdd} Credits.", null);


            // ==========================================
            // TÍCH HỢP: GỬI THÔNG BÁO CHO WEBHOOK (PAYOS)
            // ==========================================
            try
            {
                await _notificationService.SendAsync(
                    payment.UserId,
                    "Nạp ví thành công qua PayOS 💸",
                    $"Hệ thống đã ghi nhận thanh toán VietQR. Bạn đã nhận được +{totalCreditToAdd} Credits.",
                    NotificationTypes.PaymentSuccess // Hãy đổi thành Notification.PaymentSuccess nếu dùng theo cách 2
                );
            }
            catch (Exception)
            {
                // Tránh lỗi gửi thông báo làm gián đoạn phản hồi Webhook thành công với bên PayOS
            }

            return true;
        }

        public async Task<UserCredit?> GetUserCreditAsync(Guid userId)
        {
            return await _paymentRepository.GetUserCreditAsync(userId);
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

        public async Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllPaymentsAsync();
            return payments.Select(p => new PaymentHistoryDto
            {
                PaymentId = p.PaymentId,
                UserId = p.UserId,
                UserFullName = p.User?.FullName,
                UserEmail = p.User?.Email,
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