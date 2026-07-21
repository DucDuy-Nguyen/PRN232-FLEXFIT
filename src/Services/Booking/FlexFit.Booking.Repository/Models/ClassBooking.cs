using System;
using System.Collections.Generic;

namespace FlexFit.Booking.Repository.Models
{
    public class ClassBooking
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public Guid ClassId { get; set; }
        public Guid? ScheduleId { get; set; }
        public Guid BranchId { get; set; }
        public Guid GymId { get; set; }
        public string BookingCode { get; set; } = null!;
        public int CreditUsed { get; set; }

        // Snapshots of the class details
        public string GymNameSnapshot { get; set; } = null!;
        public string ClassNameSnapshot { get; set; } = null!;
        public string BranchNameSnapshot { get; set; } = null!;
        public string BranchAddressSnapshot { get; set; } = null!;
        public string CoachNameSnapshot { get; set; } = null!;
        public DateTime StartTimeSnapshot { get; set; }
        public DateTime EndTimeSnapshot { get; set; }

        public string? QrToken { get; set; }
        public DateTime? QrExpiresAt { get; set; }
        public Guid? CheckedInBy { get; set; }
        public string CheckInStatus { get; set; } = null!;
        public string Status { get; set; } = null!;
        
        public DateTime BookedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime? CheckInTime { get; set; }
        
        public int RefundCredit { get; set; }
        public bool IsReminded3h { get; set; }
        public bool IsReminded1h { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public byte[] RowVersion { get; set; } = null!;

        public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();
    }
}
