using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.API.DTOs.Requests;
using FlexFit.Payment.API.DTOs.Responses;
using FlexFit.Payment.API.Domain.Entities;

namespace FlexFit.Payment.API.Services.Interfaces
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


