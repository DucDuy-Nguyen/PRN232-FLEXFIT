using System;

namespace FlexFit.CatalogService.DTOs;

public class GymDto
{
    public Guid GymId { get; set; }
    public Guid OwnerId { get; set; }
    public string GymName { get; set; } = null!;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = null!;
    public decimal RatingAverage { get; set; }
    public int TotalReviews { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGymRequest
{
    public Guid OwnerId { get; set; }
    public string GymName { get; set; } = null!;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class UpdateGymRequest
{
    public string GymName { get; set; } = null!;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class TransferGymOwnershipDto
{
    public Guid GymId { get; set; }
    public Guid NewOwnerId { get; set; }
}
