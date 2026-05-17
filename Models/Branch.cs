using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class Branch
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

    public DateTime? UpdatedAt { get; set; }

    public virtual Gym Gym { get; set; } = null!;
    public virtual ICollection<BranchStaff> BranchStaffs { get; set; } = new List<BranchStaff>();
}
