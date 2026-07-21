using System;

namespace FlexFit.CatalogService.DTOs;

public class FavoriteGymResponse
{
    public Guid GymId { get; set; }
    public string GymName { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime LikedAt { get; set; }
}

public class FavoriteClassResponse
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string? CoachName { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int CreditCost { get; set; }
    public DateTime LikedAt { get; set; }
}
