using System;

namespace FlexFit.Payment.Domain.Entities
{
    public class UserCredit
    {
        public Guid UserCreditId { get; set; }
        public Guid UserId { get; set; }
        public int Balance { get; set; }
        public int TotalEarned { get; set; }
        public int TotalSpent { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
