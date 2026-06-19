using System;
using System.Collections.Generic;

namespace Flexfit.DTOs.AI;

public class AIUserContextDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Member Specific Info
    public int Credits { get; set; }
    public string FitnessGoal { get; set; } = string.Empty;
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public List<string> FavoriteGyms { get; set; } = new();
    public List<string> FavoriteClasses { get; set; } = new();
    public List<string> RecentBookings { get; set; } = new();
    public List<string> WorkoutHistorySummary { get; set; } = new();

    // Partner Specific Info
    public List<string> OwnedGyms { get; set; } = new();
    public string PartnerSummary { get; set; } = string.Empty;

    // Staff Specific Info
    public List<string> ManagedBranches { get; set; } = new();
    public string StaffSummary { get; set; } = string.Empty;

    // Admin Specific Info
    public string AdminSummary { get; set; } = string.Empty;
}
