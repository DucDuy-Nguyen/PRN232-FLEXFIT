using System;
using System.Collections.Generic;

namespace FlexFit.Catalog.Repository.Models;

public partial class Category
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}

