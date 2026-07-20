using System;

namespace FlexFit.CatalogService.Models;

public partial class BranchStaff
{
    public Guid StaffId { get; set; } // Scalar ID

    public Guid BranchId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
