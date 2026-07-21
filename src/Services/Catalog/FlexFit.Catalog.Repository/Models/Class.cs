using System;
using System.Collections.Generic;

namespace FlexFit.Catalog.Repository.Models;

public partial class Class
{
    public Guid ClassId { get; set; }

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

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual ICollection<FavoriteClass> FavoriteClasses { get; set; } = new List<FavoriteClass>();
}

