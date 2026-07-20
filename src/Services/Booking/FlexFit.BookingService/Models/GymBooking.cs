using System;
using System.Collections.Generic;

namespace FlexFit.BookingService.Models
{
    public class GymBooking
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
        public Guid BranchId { get; set; }
        public Guid GymId { get; set; }
        public string BookingCode { get; set; } = null!;
        public int CreditUsed { get; set; }

        // Snapshots of the session details
        public string GymNameSnapshot { get; set; } = null!;
        public string SessionNameSnapshot { get; set; } = null!;
        public string BranchNameSnapshot { get; set; } = null!;
        public string BranchAddressSnapshot { get; set; } = null!;
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
