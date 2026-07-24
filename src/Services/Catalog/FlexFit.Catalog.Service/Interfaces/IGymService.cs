using FlexFit.Catalog.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Interfaces;

public interface IGymService
{
    Task<IEnumerable<GymDto>> GetAllGymsAsync();
    Task<IEnumerable<GymDto>> GetGymsByPartnerIdAsync(Guid ownerId);
    Task<GymDto?> GetGymByIdAsync(Guid id);
    Task<Guid> CreateGymAsync(CreateGymRequest request, Guid currentUserId);
    Task UpdateGymAsync(Guid id, UpdateGymRequest request, Guid currentUserId, bool isAdmin = false);
    Task ChangeGymStatusAsync(Guid id, string status, Guid currentUserId, bool isAdmin = false);
    Task DeleteGymAsync(Guid id, Guid currentUserId, bool isAdmin = false);
    Task TransferGymOwnershipAsync(TransferGymOwnershipDto request, Guid currentUserId);
    Task<PaginatedList<GymDto>> GetGymsPagedAsync(string? search, string? status, Guid? ownerId, string? sortBy, string? sortDirection, int pageNumber, int pageSize);
}


