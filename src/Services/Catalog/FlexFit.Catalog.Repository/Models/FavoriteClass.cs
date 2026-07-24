using System;

namespace FlexFit.Catalog.Repository.Models;

public partial class FavoriteClass
{
    public Guid UserId { get; set; } // Scalar ID

    public Guid ClassId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;
}

