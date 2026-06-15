using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.DTOs.Payment;
using Flexfit.Models;
using PayOS.Models.Webhooks;

namespace Flexfit.Service
{
    public interface IPaymentService
    {
        Task<IEnumerable<CreditPackageResponse>> GetPackagesAsync();
        Task<PaymentResponse> CreatePaymentUrlAsync(Guid userId, CreatePaymentRequest request);
        Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackRequest callbackData);
        Task<bool> ProcessPayOSWebhookAsync(Webhook webhookBody);
        Task<UserCredit?> GetUserCreditAsync(Guid userId);
        Task<IEnumerable<PaymentHistoryDto>> GetUserPaymentHistoryAsync(Guid userId);
        Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentsAsync();
    }
}
