using System;

namespace FlexFit.Booking.Service.Messaging.Events
{
    // ==========================================
    // Events Published by Booking Service
    // ==========================================
    public class GymBookingCreatedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int CreditAmount { get; set; }
    }

    public class ClassBookingCreatedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public int CreditAmount { get; set; }
    }

    public class BookingCancelledEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } = null!; // "GYM" or "CLASS"
        public Guid UserId { get; set; }
        public int CreditAmount { get; set; }
    }

    public class CheckInCompletedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } = null!; // "GYM" or "CLASS"
        public Guid UserId { get; set; }
    }

    public class BookingExpiredEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } = null!; // "GYM" or "CLASS"
        public Guid UserId { get; set; }
    }

    // ==========================================
    // Events Consumed by Booking Service
    // ==========================================
    public class CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63
    {
        public Guid EventId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } = null!; // "GYM" or "CLASS"
        public Guid UserId { get; set; }
        public int CreditAmount { get; set; }
        public string TransactionId { get; set; } = null!;
    }

    public class CreditDeductionFailedEvent
    {
        public Guid EventId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } = null!;
        public Guid UserId { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class CreditRefundSucceededEvent
    {
        public Guid EventId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } = null!;
        public Guid UserId { get; set; }
        public int RefoundedAmount { get; set; }
    }

    public class ClassScheduleChangedEvent
    {
        public Guid EventId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid ClassId { get; set; }
        public DateTime NewStartTime { get; set; }
        public DateTime NewEndTime { get; set; }
    }

    public class NotificationRequestedEvent
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = "BookingReminder";
    }
}
