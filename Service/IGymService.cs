using Flexfit.DTOs;

namespace Flexfit.Services
{
    public interface IGymService
    {
        Task<IEnumerable<GymDto>> GetAllGymsAsync();
        Task<GymDto?> GetGymByIdAsync(Guid id);
        Task<Guid> CreateGymAsync(CreateGymRequest request); // Trả về Guid của Gym vừa tạo
        Task UpdateGymAsync(Guid id, UpdateGymRequest request); // Không cần trả về data
        Task ChangeGymStatusAsync(Guid id, string status);
        Task DeleteGymAsync(Guid id);
        Task TransferGymOwnershipAsync(TransferGymOwnershipDto request);
    }
}