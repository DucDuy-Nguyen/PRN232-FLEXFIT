using FlexFit.Catalog.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id);
    Task<Guid> CreateCategoryAsync(CreateCategoryRequest request);
    Task UpdateCategoryAsync(Guid id, UpdateCategoryRequest request);
    Task DeleteCategoryAsync(Guid id);
}


