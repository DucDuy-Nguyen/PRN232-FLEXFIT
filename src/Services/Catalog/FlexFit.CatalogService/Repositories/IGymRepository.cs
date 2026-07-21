using FlexFit.CatalogService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Repositories;

public interface IGymRepository
{
    Task<IEnumerable<Gym>> GetAllAsync();
    Task<IEnumerable<Gym>> GetByOwnerIdAsync(Guid ownerId);
    Task<Gym?> GetByIdAsync(Guid id);
    Task AddAsync(Gym gym);
    Task UpdateAsync(Gym gym);
    Task DeleteAsync(Guid id);
    Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId);
    Task<int> CountGymsByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<Gym>> GetOwnedGymsExceptAsync(Guid ownerId, Guid excludedGymId);
    Task<(IEnumerable<Gym> Items, int TotalCount)> GetGymsPagedAsync(string? search, string? status, Guid? ownerId, string? sortBy, string? sortDirection, int pageNumber, int pageSize);
    Task SaveChangesAsync();
}
