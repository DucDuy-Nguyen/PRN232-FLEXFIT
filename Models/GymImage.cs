using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class GymImage
{
    public Guid GymImageId { get; set; }

    public Guid GymId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Gym Gym { get; set; } = null!;
}
