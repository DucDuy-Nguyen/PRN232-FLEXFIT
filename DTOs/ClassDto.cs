using System;

namespace Flexfit.DTOs
{
    public class ClassDto
    {
        public Guid ClassId { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoachName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public int CreditCost { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? CaloriesBurnEstimate { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateClassRequest
    {
        public Guid BranchId { get; set; }
        public Guid CategoryId { get; set; }
        public string ClassName { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoachName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public int CreditCost { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? CaloriesBurnEstimate { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class UpdateClassRequest
    {
        public Guid CategoryId { get; set; }
        public string ClassName { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoachName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public int CreditCost { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? CaloriesBurnEstimate { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Status { get; set; } = null!;
    }
}
