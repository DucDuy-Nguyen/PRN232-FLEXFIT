using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class BranchImage
{
    public Guid BranchImageId { get; set; }

    public Guid BranchId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
