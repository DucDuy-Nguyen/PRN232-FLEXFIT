using FlexFit.CatalogService.DTOs;
using FlexFit.CatalogService.Helpers;
using FlexFit.CatalogService.Models;
using FlexFit.CatalogService.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Service;

public class GymService : IGymService
{
    private readonly IGymRepository _gymRepo;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GymService(IGymRepository gymRepo, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
    {
        _gymRepo = gymRepo;
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor = httpContextAccessor;
    }

    private GymDto MapToDto(Gym g)
    {
        return new GymDto
        {
            GymId = g.GymId,
            OwnerId = g.OwnerId,
            GymName = g.GymName,
            Description = g.Description,
            ThumbnailUrl = ImageHelper.GetAbsoluteUrl(g.ThumbnailUrl, _httpContextAccessor),
            PhoneNumber = g.PhoneNumber,
            Email = g.Email,
            Status = g.Status,
            RatingAverage = g.RatingAverage,
            TotalReviews = g.TotalReviews,
            CreatedAt = g.CreatedAt
        };
    }

    public async Task<IEnumerable<GymDto>> GetAllGymsAsync()
    {
        var gyms = await _gymRepo.GetAllAsync();
        return gyms.Select(MapToDto);
    }

    public async Task<IEnumerable<GymDto>> GetGymsByPartnerIdAsync(Guid ownerId)
    {
        var gyms = await _gymRepo.GetByOwnerIdAsync(ownerId);
        return gyms.Select(MapToDto);
    }

    public async Task<GymDto?> GetGymByIdAsync(Guid id)
    {
        var g = await _gymRepo.GetByIdAsync(id);
        if (g == null) return null;

        return MapToDto(g);
    }

    public async Task<Guid> CreateGymAsync(CreateGymRequest request, Guid currentUserId)
    {
        var newGym = new Gym
        {
            GymId = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            GymName = request.GymName,
            Description = request.Description,
            ThumbnailUrl = ImageHelper.SaveBase64Image(request.ThumbnailUrl, "gyms", "gym", _webHostEnvironment),
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Status = "Pending",
            RatingAverage = 0,
            TotalReviews = 0,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _gymRepo.AddAsync(newGym);
        await _gymRepo.SaveChangesAsync();

        return newGym.GymId;
    }

    public async Task UpdateGymAsync(Guid id, UpdateGymRequest request, Guid currentUserId, bool isAdmin = false)
    {
        if (!isAdmin)
        {
            var isOwner = await _gymRepo.CheckGymOwnershipAsync(id, currentUserId);
            if (!isOwner)
                throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này.");
        }

        var gym = await _gymRepo.GetByIdAsync(id);
        if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

        gym.GymName = request.GymName;
        gym.Description = request.Description;
        gym.ThumbnailUrl = ImageHelper.SaveBase64Image(request.ThumbnailUrl, "gyms", "gym", _webHostEnvironment);
        gym.PhoneNumber = request.PhoneNumber;
        gym.Email = request.Email;
        gym.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _gymRepo.UpdateAsync(gym);
    }

    public async Task ChangeGymStatusAsync(Guid id, string status, Guid currentUserId, bool isAdmin = false)
    {
        if (!isAdmin)
        {
            var isOwner = await _gymRepo.CheckGymOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này.");
        }

        var gym = await _gymRepo.GetByIdAsync(id);
        if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

        gym.Status = status;
        gym.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _gymRepo.UpdateAsync(gym);
    }

    public async Task DeleteGymAsync(Guid id, Guid currentUserId, bool isAdmin = false)
    {
        if (!isAdmin)
        {
            var isOwner = await _gymRepo.CheckGymOwnershipAsync(id, currentUserId);
            if (!isOwner)
                throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này.");
        }

        var gym = await _gymRepo.GetByIdAsync(id);
        if (gym == null)
            throw new KeyNotFoundException("Không tìm thấy phòng tập.");

        await _gymRepo.DeleteAsync(id);
    }

    public async Task TransferGymOwnershipAsync(TransferGymOwnershipDto request, Guid currentUserId)
    {
        var isOwner = await _gymRepo.CheckGymOwnershipAsync(request.GymId, currentUserId);
        if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này để thực hiện chuyển nhượng.");

        var gym = await _gymRepo.GetByIdAsync(request.GymId);
        if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

        if (gym.OwnerId == request.NewOwnerId)
            throw new ArgumentException($"Người dùng đã là chủ sở hữu phòng tập này rồi.");

        gym.OwnerId = request.NewOwnerId;
        gym.UpdatedAt = DateTimeHelper.GetVietnamTime();
        await _gymRepo.UpdateAsync(gym);
    }

    public async Task<PaginatedList<GymDto>> GetGymsPagedAsync(string? search, string? status, Guid? ownerId, string? sortBy, string? sortDirection, int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 10;

        var (items, totalCount) = await _gymRepo.GetGymsPagedAsync(search, status, ownerId, sortBy, sortDirection, pageNumber, pageSize);
        var dtos = items.Select(MapToDto).ToList();

        return new PaginatedList<GymDto>(dtos, totalCount, pageNumber, pageSize);
    }
}
