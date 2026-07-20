using System;
using System.Collections.Generic;

namespace FlexFit.CatalogService.Models;

public partial class GymAmenity
{
    public Guid AmenityId { get; set; }

    public string AmenityName { get; set; } = null!;

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
