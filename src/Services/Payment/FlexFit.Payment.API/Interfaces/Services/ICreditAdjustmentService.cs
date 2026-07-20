using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Interfaces.Services
{
    public interface ICreditAdjustmentService
    {
        Task DeductCreditAsync(Guid bookingId, Guid userId, int creditCost, string referenceType, string description);
        Task RefundCreditAsync(Guid bookingId, Guid userId, int refundCredit, string referenceType, string description);
    }
}
