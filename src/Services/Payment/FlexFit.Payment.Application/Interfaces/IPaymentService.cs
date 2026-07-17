using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.Application.DTOs.Payment;
using FlexFit.Payment.Domain.Entities;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<CreditPackageResponse>> GetPackagesAsync();
        Task<PaymentResponse> CreatePaymentUrlAsync(Guid userId, CreatePaymentRequest request);
        Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackRequest callbackData);
        Task<bool> ProcessPayOSWebhookAsync(object webhookBody);
        Task<UserCredit?> GetUserCreditAsync(Guid userId);
        Task<IEnumerable<PaymentHistoryDto>> GetUserPaymentHistoryAsync(Guid userId);
        Task<PaymentHistoryDto?> GetPaymentStatusAsync(Guid paymentId, Guid userId);
        Task<IEnumerable<PaymentHistoryDto>> GetAllPaymentsAsync();
    }
}
