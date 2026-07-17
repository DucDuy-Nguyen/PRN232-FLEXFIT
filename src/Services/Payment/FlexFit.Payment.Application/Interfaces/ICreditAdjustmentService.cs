using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface ICreditAdjustmentService
    {
        Task DeductCreditAsync(Guid bookingId, Guid userId, int creditCost, string referenceType, string description);
        Task RefundCreditAsync(Guid bookingId, Guid userId, int refundCredit, string referenceType, string description);
    }
}
