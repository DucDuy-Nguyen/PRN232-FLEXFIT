using System;

namespace FlexFit.Catalog.Repository.Models;

public partial class BranchStaff
{
    public Guid StaffId { get; set; } // Scalar ID

    public Guid BranchId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}

