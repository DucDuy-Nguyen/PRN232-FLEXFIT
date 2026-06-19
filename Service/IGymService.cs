using Flexfit.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Services
{
    public interface IGymService
    {
        Task<IEnumerable<GymDto>> GetAllGymsAsync();
        Task<IEnumerable<GymDto>> GetGymsByPartnerIdAsync(Guid ownerId);
        Task<GymDto?> GetGymByIdAsync(Guid id);
        Task<Guid> CreateGymAsync(CreateGymRequest request, Guid currentUserId); // 👈 Thêm currentUserId
        Task UpdateGymAsync(Guid id, UpdateGymRequest request, Guid currentUserId); // 👈 Thêm currentUserId
        Task ChangeGymStatusAsync(Guid id, string status, Guid currentUserId, bool isAdmin = false); // 👈 Thêm currentUserId và isAdmin
        Task DeleteGymAsync(Guid id, Guid currentUserId); // 👈 Thêm currentUserId
        Task TransferGymOwnershipAsync(TransferGymOwnershipDto request, Guid currentUserId); // 👈 Thêm currentUserId
    }
}