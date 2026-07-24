using System;

namespace FlexFit.Catalog.Repository.Models;

public partial class FavoriteGym
{
    public Guid UserId { get; set; } // Scalar ID

    public Guid GymId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Gym Gym { get; set; } = null!;
}

