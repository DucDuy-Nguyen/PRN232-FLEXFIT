using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class BranchStaff
{
    public Guid StaffId { get; set; }

    public Guid BranchId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
