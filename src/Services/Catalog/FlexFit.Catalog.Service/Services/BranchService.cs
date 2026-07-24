using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.DTOs;
using FlexFit.Catalog.Service.Helpers;
using FlexFit.Catalog.Repository.Models;
using FlexFit.Catalog.Repository.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Services;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepo;
    private readonly IRedisPublisher _redisPublisher;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BranchService> _logger;

    public BranchService(
        IBranchRepository branchRepo,
        IRedisPublisher redisPublisher,
        IWebHostEnvironment webHostEnvironment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BranchService> logger)
    {
        _branchRepo = branchRepo;
        _redisPublisher = redisPublisher;
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private async Task<bool> CheckBranchManagementPermissionAsync(Guid branchId, Guid userId)
    {
        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(branchId, userId);
        if (isOwner) return true;

        var isStaffHere = await _branchRepo.IsStaffInBranchAsync(userId, branchId);
        if (isStaffHere) return true;

        return false;
    }

    public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
    {
        var branches = await _branchRepo.GetAllAsync();
        return branches.Select(MapToDto);
    }

    public async Task<IEnumerable<BranchDto>> GetBranchesByPartnerIdAsync(Guid ownerId)
    {
        var branches = await _branchRepo.GetByOwnerIdAsync(ownerId);
        return branches.Select(MapToDto);
    }

    public async Task<BranchDto?> GetBranchByIdAsync(Guid id)
    {
        var b = await _branchRepo.GetByIdAsync(id);
        if (b == null) return null;

        return MapToDto(b);
    }

    public async Task<Guid> CreateBranchAsync(CreateBranchRequest request, Guid currentUserId)
    {
        var isOwner = await _branchRepo.CheckGymOwnershipAsync(request.GymId, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ của phòng gym này nên không thể tạo chi nhánh.");

        var newBranch = new Branch
        {
            BranchId = Guid.NewGuid(),
            GymId = request.GymId,
            BranchName = request.BranchName,
            Address = request.Address,
            City = request.City,
            District = request.District,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime,
            ThumbnailUrl = ImageHelper.SaveBase64Image(request.ThumbnailUrl, "branches", "branch", _webHostEnvironment),
            CreditCost = request.CreditCost,
            IsActive = true,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _branchRepo.AddAsync(newBranch);
        return newBranch.BranchId;
    }

    public async Task UpdateBranchAsync(Guid id, UpdateBranchRequest request, Guid currentUserId)
    {
        var hasPermission = await CheckBranchManagementPermissionAsync(id, currentUserId);
        if (!hasPermission) throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

        branch.BranchName = request.BranchName;
        branch.Address = request.Address;
        branch.City = request.City;
        branch.District = request.District;
        branch.OpenTime = request.OpenTime;
        branch.CloseTime = request.CloseTime;
        branch.ThumbnailUrl = ImageHelper.SaveBase64Image(request.ThumbnailUrl, "branches", "branch", _webHostEnvironment);
        branch.CreditCost = request.CreditCost;
        branch.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _branchRepo.UpdateAsync(branch);
    }

    public async Task ChangeBranchStatusAsync(Guid id, bool isActive, Guid currentUserId)
    {
        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(id, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

        branch.IsActive = isActive;
        branch.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _branchRepo.UpdateAsync(branch);
    }

    public async Task DeleteBranchAsync(Guid id, Guid currentUserId)
    {
        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(id, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền xóa chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

        await _branchRepo.DeleteAsync(id);
    }

    public async Task UpdateBranchAmenitiesAsync(Guid branchId, UpdateBranchAmenitiesRequest request, Guid currentUserId)
    {
        var hasPermission = await CheckBranchManagementPermissionAsync(branchId, currentUserId);
        if (!hasPermission) throw new UnauthorizedAccessException("Bạn không có quyền quản lý tiện ích tại chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(branchId);
        if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

        if (branch.Amenities == null)
        {
            branch.Amenities = new List<GymAmenity>();
        }
        else
        {
            branch.Amenities.Clear();
        }

        if (request.AmenityIds != null && request.AmenityIds.Any())
        {
            foreach (var amenityId in request.AmenityIds)
            {
                var amenity = await _branchRepo.GetAmenityByIdAsync(amenityId);
                if (amenity != null)
                {
                    branch.Amenities.Add(amenity);
                }
            }
        }

        branch.UpdatedAt = DateTimeHelper.GetVietnamTime();
        await _branchRepo.SaveChangesAsync();
    }

    public async Task AssignStaffToBranchAsync(AssignStaffDto dto, Guid currentUserId)
    {
        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(dto.BranchId, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền quản lý nhân sự tại chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
        if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại trên hệ thống.");

        var isAlreadyStaffHere = await _branchRepo.IsStaffInBranchAsync(dto.UserId, dto.BranchId);
        if (isAlreadyStaffHere) throw new ArgumentException("Người này đã là nhân viên của chi nhánh này rồi!");

        await _branchRepo.RemoveStaffFromAllBranchesAsync(dto.UserId);
        await _branchRepo.AddBranchStaffAsync(new BranchStaff { StaffId = dto.UserId, BranchId = dto.BranchId, AssignedAt = DateTimeHelper.GetVietnamTime() });
        await _branchRepo.SaveChangesAsync();

        // Publish event to Redis Stream
        await _redisPublisher.PublishAsync("catalog-stream", "StaffAssignedToBranchEvent", new
        {
            StaffId = dto.UserId,
            BranchId = dto.BranchId
        });
    }

    public async Task AssignStaffToBranchByEmailAsync(AssignStaffByEmailDto dto, Guid currentUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException("Vui lòng nhập email nhân viên.");

        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(dto.BranchId, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền quản lý nhân sự tại chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
        if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại trên hệ thống.");

        // In microservice boundary, the User DB is not available.
        // We will deterministically generate a Guid for the email in dev environment, or lookup via a mock.
        // For production, this should call the Identity Service. We simulate it here by generating a Guid.
        // Deterministic Guid generation from email string
        byte[] emailBytes = System.Text.Encoding.UTF8.GetBytes(dto.Email.Trim().ToLower());
        byte[] hashBytes = System.Security.Cryptography.MD5.HashData(emailBytes);
        Guid staffId = new Guid(hashBytes);

        var isAlreadyStaffHere = await _branchRepo.IsStaffInBranchAsync(staffId, dto.BranchId);
        if (isAlreadyStaffHere) throw new ArgumentException("Người này đã là nhân viên của chi nhánh này rồi!");

        await _branchRepo.RemoveStaffFromAllBranchesAsync(staffId);
        await _branchRepo.AddBranchStaffAsync(new BranchStaff { StaffId = staffId, BranchId = dto.BranchId, AssignedAt = DateTimeHelper.GetVietnamTime() });
        await _branchRepo.SaveChangesAsync();

        // Publish event to Redis Stream
        await _redisPublisher.PublishAsync("catalog-stream", "StaffAssignedToBranchEvent", new
        {
            StaffId = staffId,
            BranchId = dto.BranchId
        });
    }

    public async Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId, Guid currentUserId)
    {
        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(branchId, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không quyền gỡ nhân sự tại chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(branchId);
        if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

        var branchStaff = await _branchRepo.GetBranchStaffAsync(staffId, branchId);
        if (branchStaff == null) throw new KeyNotFoundException("Nhân viên này hiện không thuộc chi nhánh này.");

        await _branchRepo.RemoveBranchStaffAsync(branchStaff);
        await _branchRepo.SaveChangesAsync();

        // Publish event to Redis Stream
        await _redisPublisher.PublishAsync("catalog-stream", "StaffRemovedFromBranchEvent", new
        {
            StaffId = staffId,
            BranchId = branchId
        });
    }

    public async Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto, Guid currentUserId)
    {
        var isOwner = await _branchRepo.CheckBranchOwnershipAsync(dto.BranchId, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền chuyển giao nhân sự tại chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
        if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

        var oldAssignment = await _branchRepo.GetBranchStaffByBranchIdAsync(dto.BranchId);
        if (oldAssignment != null)
        {
            Guid oldStaffId = oldAssignment.StaffId;
            if (oldStaffId == dto.NewStaffId) return;

            await _branchRepo.RemoveBranchStaffAsync(oldAssignment);
            
            // Publish event for staff removal
            await _redisPublisher.PublishAsync("catalog-stream", "StaffRemovedFromBranchEvent", new
            {
                StaffId = oldStaffId,
                BranchId = dto.BranchId
            });
        }

        await _branchRepo.RemoveStaffFromAllBranchesAsync(dto.NewStaffId);
        await _branchRepo.AddBranchStaffAsync(new BranchStaff { StaffId = dto.NewStaffId, BranchId = dto.BranchId, AssignedAt = DateTimeHelper.GetVietnamTime() });
        await _branchRepo.SaveChangesAsync();

        // Publish event for staff assignment
        await _redisPublisher.PublishAsync("catalog-stream", "StaffAssignedToBranchEvent", new
        {
            StaffId = dto.NewStaffId,
            BranchId = dto.BranchId
        });
    }

    public async Task<IEnumerable<GymAmenityDto>> GetAllAmenitiesAsync()
    {
        var amenities = await _branchRepo.GetAllAmenitiesAsync();
        return amenities.Select(a => new GymAmenityDto
        {
            AmenityId = a.AmenityId,
            AmenityName = a.AmenityName
        });
    }

    public async Task<Guid> CreateAmenityAsync(string amenityName)
    {
        if (string.IsNullOrWhiteSpace(amenityName))
            throw new ArgumentException("Tên tiện ích không được để trống.");

        var exists = await _branchRepo.AmenityExistsAsync(amenityName);
        if (exists)
            throw new ArgumentException("Tên tiện ích này đã tồn tại trên hệ thống.");

        var newAmenity = new GymAmenity
        {
            AmenityId = Guid.NewGuid(),
            AmenityName = amenityName.Trim()
        };

        await _branchRepo.AddAmenityAsync(newAmenity);
        return newAmenity.AmenityId;
    }

    public async Task UpdateAmenityAsync(Guid amenityId, string newAmenityName)
    {
        if (string.IsNullOrWhiteSpace(newAmenityName))
            throw new ArgumentException("Tên tiện ích không được để trống.");

        var amenity = await _branchRepo.GetAmenityByIdAsync(amenityId);
        if (amenity == null)
            throw new KeyNotFoundException("Không tìm thấy tiện ích này trên hệ thống.");

        var formattedName = newAmenityName.Trim();

        if (!string.Equals(amenity.AmenityName, formattedName, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _branchRepo.AmenityExistsAsync(formattedName);
            if (exists)
                throw new ArgumentException("Tên tiện ích này đã tồn tại trên hệ thống.");
        }

        amenity.AmenityName = formattedName;
        await _branchRepo.UpdateAmenityAsync(amenity);
    }

    public async Task DeleteAmenityAsync(Guid amenityId)
    {
        var amenity = await _branchRepo.GetAmenityByIdAsync(amenityId);
        if (amenity == null)
            throw new KeyNotFoundException("Không tìm thấy tiện ích này trên hệ thống.");

        await _branchRepo.DeleteAmenityAsync(amenity);
    }

    public async Task UpdateBranchImagesAsync(Guid branchId, UpdateBranchImagesRequest request, Guid currentUserId)
    {
        var hasPermission = await CheckBranchManagementPermissionAsync(branchId, currentUserId);
        if (!hasPermission) throw new UnauthorizedAccessException("Bạn không có quyền quản lý hình ảnh tại chi nhánh này.");

        var branch = await _branchRepo.GetByIdAsync(branchId);
        if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

        var newImages = new List<BranchImage>();
        if (request.Images != null && request.Images.Any())
        {
            foreach (var imgReq in request.Images)
            {
                if (string.IsNullOrWhiteSpace(imgReq.ImageUrl)) continue;

                string? savedUrl = ImageHelper.SaveBase64Image(imgReq.ImageUrl.Trim(), "branches", "branch_detail", _webHostEnvironment);
                if (string.IsNullOrWhiteSpace(savedUrl)) continue;

                newImages.Add(new BranchImage
                {
                    BranchImageId = Guid.NewGuid(),
                    BranchId = branchId,
                    ImageUrl = savedUrl,
                    DisplayOrder = imgReq.DisplayOrder
                });
            }
        }

        branch.UpdatedAt = DateTimeHelper.GetVietnamTime();
        await _branchRepo.UpdateBranchImagesDbAsync(branchId, newImages);
    }

    private BranchDto MapToDto(Branch b)
    {
        return new BranchDto
        {
            BranchId = b.BranchId,
            GymId = b.GymId,
            BranchName = b.BranchName,
            Address = b.Address,
            City = b.City,
            District = b.District,
            OpenTime = b.OpenTime,
            CloseTime = b.CloseTime,
            ThumbnailUrl = ImageHelper.GetAbsoluteUrl(b.ThumbnailUrl, _httpContextAccessor),
            CreditCost = b.CreditCost,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            Staffs = b.BranchStaffs?.Select(bs => new StaffInfoDto
            {
                StaffId = bs.StaffId,
                FullName = "Staff Id: " + bs.StaffId.ToString().Substring(0, 8)
            }).ToList() ?? new List<StaffInfoDto>(),

            Amenities = b.Amenities?.Select(a => new GymAmenityDto
            {
                AmenityId = a.AmenityId,
                AmenityName = a.AmenityName
            }).ToList() ?? new List<GymAmenityDto>(),

            Images = b.BranchImages?.Select(i => new BranchImageDto
            {
                BranchImageId = i.BranchImageId,
                ImageUrl = ImageHelper.GetAbsoluteUrl(i.ImageUrl, _httpContextAccessor),
                DisplayOrder = i.DisplayOrder
            }).OrderBy(i => i.DisplayOrder).ToList() ?? new List<BranchImageDto>()
        };
    }
}


