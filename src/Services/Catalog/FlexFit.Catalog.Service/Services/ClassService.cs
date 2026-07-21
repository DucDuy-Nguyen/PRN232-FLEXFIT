using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.DTOs;
using FlexFit.Catalog.Service.Helpers;
using FlexFit.Catalog.Repository.Models;
using FlexFit.Catalog.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Services;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepo;

    public ClassService(IClassRepository classRepo)
    {
        _classRepo = classRepo;
    }

    public async Task<IEnumerable<ClassDto>> GetAllClassesAsync()
    {
        var classes = await _classRepo.GetAllAsync();
        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetClassesByBranchAsync(Guid branchId)
    {
        var classes = await _classRepo.GetByBranchIdAsync(branchId);
        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetClassesByStaffIdAsync(Guid staffId)
    {
        var classes = await _classRepo.GetClassesByStaffIdAsync(staffId);
        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetClassesByPartnerIdAsync(Guid partnerId)
    {
        var classes = await _classRepo.GetClassesByPartnerIdAsync(partnerId);
        return classes.Select(MapToDto);
    }

    public async Task<ClassDto?> GetClassByIdAsync(Guid id)
    {
        var c = await _classRepo.GetByIdAsync(id);
        if (c == null) return null;
        return MapToDto(c);
    }

    public async Task<Guid> CreateClassAsync(CreateClassRequest request, Guid currentUserId)
    {
        if (string.IsNullOrWhiteSpace(request.ClassName))
            throw new ArgumentException("Tên lớp học không được để trống.");

        if (request.StartTime >= request.EndTime)
            throw new ArgumentException("Thời gian bắt đầu phải trước thời gian kết thúc.");

        if (request.Capacity <= 0)
            throw new ArgumentException("Sức chứa phải lớn hơn 0.");

        if (request.CreditCost < 0)
            throw new ArgumentException("Chi phí tín dụng không được nhỏ hơn 0.");

        var branchExists = await _classRepo.BranchExistsAsync(request.BranchId);
        if (!branchExists)
            throw new KeyNotFoundException("Chi nhánh liên kết không tồn tại trên hệ thống.");

        var isOwner = await _classRepo.CheckBranchOwnershipAsync(request.BranchId, currentUserId);
        if (!isOwner)
            throw new UnauthorizedAccessException("Bạn không phải chủ của phòng gym sở hữu chi nhánh này nên không thể tạo lớp học.");

        var categoryExists = await _classRepo.CategoryExistsAsync(request.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException("Thể loại lớp học không tồn tại trên hệ thống.");

        var newClass = new Class
        {
            ClassId = Guid.NewGuid(),
            BranchId = request.BranchId,
            CategoryId = request.CategoryId,
            ClassName = request.ClassName.Trim(),
            Description = request.Description,
            CoachName = request.CoachName,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            CreditCost = request.CreditCost,
            DifficultyLevel = request.DifficultyLevel,
            CaloriesBurnEstimate = request.CaloriesBurnEstimate,
            ThumbnailUrl = request.ThumbnailUrl,
            Status = "Open",
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _classRepo.AddAsync(newClass);
        return newClass.ClassId;
    }

    public async Task UpdateClassAsync(Guid id, UpdateClassRequest request, Guid currentUserId)
    {
        var existingClass = await _classRepo.GetByIdAsync(id);
        if (existingClass == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        var isOwner = await _classRepo.CheckClassOwnershipAsync(id, currentUserId);
        if (!isOwner)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa lớp học này.");

        if (string.IsNullOrWhiteSpace(request.ClassName))
            throw new ArgumentException("Tên lớp học không được để trống.");

        if (request.StartTime >= request.EndTime)
            throw new ArgumentException("Thời gian bắt đầu phải trước thời gian kết thúc.");

        if (request.Capacity <= 0)
            throw new ArgumentException("Sức chứa phải lớn hơn 0.");

        if (request.CreditCost < 0)
            throw new ArgumentException("Chi phí tín dụng không được nhỏ hơn 0.");

        var categoryExists = await _classRepo.CategoryExistsAsync(request.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException("Thể loại lớp học không tồn tại trên hệ thống.");

        var validStatuses = new[] { "Open", "Cancelled", "Completed" };
        if (!validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Trạng thái không hợp lệ. Trạng thái phải là: 'Open', 'Cancelled', hoặc 'Completed'.");

        existingClass.CategoryId = request.CategoryId;
        existingClass.ClassName = request.ClassName.Trim();
        existingClass.Description = request.Description;
        existingClass.CoachName = request.CoachName;
        existingClass.StartTime = request.StartTime;
        existingClass.EndTime = request.EndTime;
        existingClass.Capacity = request.Capacity;
        existingClass.CreditCost = request.CreditCost;
        existingClass.DifficultyLevel = request.DifficultyLevel;
        existingClass.CaloriesBurnEstimate = request.CaloriesBurnEstimate;
        existingClass.ThumbnailUrl = request.ThumbnailUrl;
        existingClass.Status = request.Status;
        existingClass.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _classRepo.UpdateAsync(existingClass);
    }

    public async Task ChangeClassStatusAsync(Guid id, string status, Guid currentUserId)
    {
        var existingClass = await _classRepo.GetByIdAsync(id);
        if (existingClass == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        var isOwner = await _classRepo.CheckClassOwnershipAsync(id, currentUserId);
        if (!isOwner)
            throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái lớp học này.");

        var validStatuses = new[] { "Open", "Cancelled", "Completed" };
        if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Trạng thái không hợp lệ. Trạng thái phải là: 'Open', 'Cancelled', hoặc 'Completed'.");

        existingClass.Status = status;
        existingClass.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _classRepo.UpdateAsync(existingClass);
    }

    public async Task DeleteClassAsync(Guid id, Guid currentUserId)
    {
        var existingClass = await _classRepo.GetByIdAsync(id);
        if (existingClass == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        var isOwner = await _classRepo.CheckClassOwnershipAsync(id, currentUserId);
        if (!isOwner)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa lớp học này.");

        // NOTE: In microservice architecture, checking for active bookings belongs to the Booking Service.
        // We delete the class from catalog and rely on downstream services or database cascade/notifications if any.
        await _classRepo.DeleteAsync(id);
    }

    private ClassDto MapToDto(Class c)
    {
        return new ClassDto
        {
            ClassId = c.ClassId,
            BranchId = c.BranchId,
            BranchName = c.Branch?.BranchName ?? "N/A",
            CategoryId = c.CategoryId,
            CategoryName = c.Category?.CategoryName ?? "N/A",
            ClassName = c.ClassName,
            Description = c.Description,
            CoachName = c.CoachName,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Capacity = c.Capacity,
            CreditCost = c.CreditCost,
            DifficultyLevel = c.DifficultyLevel,
            CaloriesBurnEstimate = c.CaloriesBurnEstimate,
            ThumbnailUrl = c.ThumbnailUrl,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }

    public async Task<PaginatedList<ClassDto>> GetClassesPagedAsync(string? search, Guid? branchId, Guid? categoryId, string? status, string? sortBy, string? sortDirection, int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 10;

        var (items, totalCount) = await _classRepo.GetClassesPagedAsync(search, branchId, categoryId, status, sortBy, sortDirection, pageNumber, pageSize);
        var dtos = items.Select(MapToDto).ToList();

        return new PaginatedList<ClassDto>(dtos, totalCount, pageNumber, pageSize);
    }
}


