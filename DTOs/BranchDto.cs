namespace Flexfit.DTOs
{
    public class BranchDto
    {
        public Guid BranchId { get; set; }
        public Guid GymId { get; set; }
        public string BranchName { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBranchRequest
    {
        public Guid GymId { get; set; }
        public string BranchName { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class UpdateBranchRequest
    {
        public string BranchName { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}