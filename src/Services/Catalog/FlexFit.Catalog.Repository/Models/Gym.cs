using System;
using System.Collections.Generic;

namespace FlexFit.Catalog.Repository.Models;

public partial class Gym
{
    public Guid GymId { get; set; }

    public Guid OwnerId { get; set; } // Scalar ID

    public string GymName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string Status { get; set; } = null!;

    public decimal RatingAverage { get; set; }

    public int TotalReviews { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<FavoriteGym> FavoriteGyms { get; set; } = new List<FavoriteGym>();

    public virtual ICollection<GymImage> GymImages { get; set; } = new List<GymImage>();
}

