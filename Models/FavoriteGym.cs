using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class FavoriteGym
{
    public Guid UserId { get; set; }

    public Guid GymId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Gym Gym { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
