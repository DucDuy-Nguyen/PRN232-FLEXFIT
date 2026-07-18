using System;

namespace FlexFit.Payment.API.Contracts.Events
{
    public class CreditDeductionRequested
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int CreditCost { get; set; }
        public string ReferenceType { get; set; } = null!; // e.g. "GymBooking" or "ClassBooking"
        public string Description { get; set; } = null!;
    }

    public class CreditDeductionSucceeded
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int CreditCost { get; set; }
        public int NewBalance { get; set; }
    }

    public class CreditDeductionFailed
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int CreditCost { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class CreditRefundRequested
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int RefundCredit { get; set; }
        public string ReferenceType { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class CreditRefundSucceeded
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int RefundCredit { get; set; }
        public int NewBalance { get; set; }
    }

    public class CreditRefundFailed
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int RefundCredit { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class PaymentCompleted
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public decimal AmountPaid { get; set; }
        public int CreditsAdded { get; set; }
        public int NewBalance { get; set; }
    }

    public class PaymentFailed
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class CreditAdjusted
    {
        public Guid UserId { get; set; }
        public int Amount { get; set; }
        public int NewBalance { get; set; }
        public string Description { get; set; } = null!;
    }
}
