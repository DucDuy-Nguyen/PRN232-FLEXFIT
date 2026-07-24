using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Booking.Service.ExternalServices.Catalog
{
    public interface ICatalogServiceClient
    {
        Task<CatalogSessionDetails?> GetGymSessionDetailsAsync(Guid sessionId);
        Task<CatalogClassDetails?> GetClassDetailsAsync(Guid classId);
        Task<bool> VerifyStaffPermissionAsync(Guid staffId, Guid branchId);
        Task<IEnumerable<Guid>> GetManagedBranchIdsAsync(Guid managerId, string role);
    }

    public class CatalogSessionDetails
    {
        public Guid SessionId { get; set; }
        public Guid GymId { get; set; }
        public string GymName { get; set; } = null!;
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public string BranchAddress { get; set; } = null!;
        public string SessionName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public int CreditCost { get; set; }
        public string Status { get; set; } = "Open";
    }

    public class CatalogClassDetails
    {
        public Guid ClassId { get; set; }
        public Guid? ScheduleId { get; set; }
        public Guid GymId { get; set; }
        public string GymName { get; set; } = null!;
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public string BranchAddress { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string CoachName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public int CreditCost { get; set; }
        public string Status { get; set; } = "Open";
    }
}
