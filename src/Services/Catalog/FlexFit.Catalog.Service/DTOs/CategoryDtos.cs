using System;

namespace FlexFit.Catalog.Service.DTOs;

public class CategoryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
}

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateCategoryRequest
{
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
}

