using System;

namespace FlexFit.CatalogService.Models;

public partial class GymImage
{
    public Guid GymImageId { get; set; }

    public Guid GymId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Gym Gym { get; set; } = null!;
}
