using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.DTOs;
using FlexFit.Catalog.Repository.Models;
using FlexFit.Catalog.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Services;

public class CategoryService : ICategoryService
{
    private readonly IGenericRepository<Category> _categoryRepo;
    private readonly IGenericRepository<Class> _classRepo;

    public CategoryService(
        IGenericRepository<Category> categoryRepo,
        IGenericRepository<Class> classRepo)
    {
        _categoryRepo = categoryRepo;
        _classRepo = classRepo;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepo.GetAllAsync();
        return categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var c = await _categoryRepo.GetByIdAsync(id);
        if (c == null) return null;
        return new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description
        };
    }

    public async Task<Guid> CreateCategoryAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
            throw new ArgumentException("Tên danh mục không được để trống.");

        var trimmedName = request.CategoryName.Trim().ToLower();
        var existing = await _categoryRepo.FindAsync(c => c.CategoryName.ToLower() == trimmedName);
        if (existing.Any())
            throw new ArgumentException("Tên danh mục đã tồn tại trên hệ thống.");

        var category = new Category
        {
            CategoryId = Guid.NewGuid(),
            CategoryName = request.CategoryName.Trim(),
            Description = request.Description?.Trim()
        };

        await _categoryRepo.AddAsync(category);
        await _categoryRepo.SaveChangesAsync();

        return category.CategoryId;
    }

    public async Task UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Không tìm thấy danh mục lớp học.");

        if (string.IsNullOrWhiteSpace(request.CategoryName))
            throw new ArgumentException("Tên danh mục không được để trống.");

        var trimmedName = request.CategoryName.Trim().ToLower();
        var existing = await _categoryRepo.FindAsync(c => c.CategoryId != id && c.CategoryName.ToLower() == trimmedName);
        if (existing.Any())
            throw new ArgumentException("Tên danh mục đã tồn tại.");

        category.CategoryName = request.CategoryName.Trim();
        category.Description = request.Description?.Trim();

        _categoryRepo.Update(category);
        await _categoryRepo.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Không tìm thấy danh mục lớp học.");

        var classes = await _classRepo.FindAsync(c => c.CategoryId == id);
        if (classes.Any())
            throw new InvalidOperationException("Không thể xóa danh mục này vì đang có lớp học thuộc danh mục này.");

        _categoryRepo.Delete(category);
        await _categoryRepo.SaveChangesAsync();
    }
}


