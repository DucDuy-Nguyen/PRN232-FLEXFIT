using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.DTOs.Payment;
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

        public PaymentService(IPaymentRepository paymentRepository, PayOSClient payOSClient)
        {
            _paymentRepository = paymentRepository;
            _payOSClient = payOSClient;
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
                // Generate unique numeric order code (ticks in milliseconds is perfectly unique and under JavaScript safe integer)
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
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.CreatePaymentAsync(payment);

            // Generate payment URL based on method (MOCK, VNPAY, MOMO, PAYOS)
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
                        CancelUrl = "https://localhost:7115/payment/cancel",
                        ReturnUrl = "https://localhost:7115/payment/success"
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
                    // TODO: Tích hợp SDK VNPAY tại đây trong tương lai
                    paymentUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?mock=true&paymentId={paymentId}&amount={package.Price}";
                    break;
                case "MOMO":
                    // TODO: Tích hợp SDK MoMo tại đây trong tương lai
                    paymentUrl = $"https://test-payment.momo.vn/v2/gateway/api/create?mock=true&paymentId={paymentId}&amount={package.Price}";
                    break;
                case "MOCK":
                default:
                    // Hệ thống Mock giả lập thanh toán
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

            // Tránh xử lý lại giao dịch đã thành công/thất bại
            if (payment.Status != "Pending")
            {
                return payment.Status == "Success";
            }

            if (callbackData.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
            {
                // 1. Cập nhật trạng thái thanh toán
                await _paymentRepository.UpdatePaymentStatusAsync(
                    payment.PaymentId, 
                    "Success", 
                    callbackData.ProviderTransactionCode ?? $"MOCK_TXN_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
                );

                // 2. Lấy thông tin gói
                var package = payment.Package;
                int totalCreditToAdd = package.CreditAmount + package.BonusCredit;

                // 3. Cập nhật số dư UserCredit
                var userCredit = await _paymentRepository.GetUserCreditAsync(payment.UserId);
                int balanceBefore = 0;

                if (userCredit == null)
                {
                    // Tạo mới ví nếu chưa có (mặc dù đã có cơ chế tạo khi Register)
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

                // 4. Lưu lịch sử giao dịch tín dụng (CreditTransaction)
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
            // 1. Xác minh chữ ký bảo mật từ PayOS
            var verifiedData = await _payOSClient.Webhooks.VerifyAsync(webhookBody);
            if (verifiedData == null)
            {
                throw new Exception("Xác thực chữ ký PayOS thất bại hoặc dữ liệu không hợp lệ.");
            }

            // 2. Tìm giao dịch trong CSDL theo OrderCode (lưu trong ProviderTransactionCode)
            var payment = await _paymentRepository.GetPaymentByTransactionCodeAsync(verifiedData.OrderCode.ToString());
            if (payment == null)
            {
                return false;
            }

            // 3. Tránh xử lý trùng lặp giao dịch đã hoàn tất
            if (payment.Status != "Pending")
            {
                return true;
            }

            // 4. Cập nhật trạng thái thanh toán thành công
            await _paymentRepository.UpdatePaymentStatusAsync(
                payment.PaymentId, 
                "Success", 
                verifiedData.OrderCode.ToString()
            );

            // 5. Lấy gói nạp và tính toán tín dụng
            var package = payment.Package;
            int totalCreditToAdd = package.CreditAmount + package.BonusCredit;

            // 6. Cập nhật ví UserCredit
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

            // 7. Ghi nhận lịch sử giao dịch tín dụng (Deposit)
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
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddCreditTransactionAsync(transaction);
            return true;
        }

        public async Task<UserCredit?> GetUserCreditAsync(Guid userId)
        {
            return await _paymentRepository.GetUserCreditAsync(userId);
        }
    }
}
