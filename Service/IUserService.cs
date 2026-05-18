using Flexfit.DTOs;

namespace Flexfit.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task ChangeUserStatusAsync(Guid id, bool isActive);
        Task DeleteUserAsync(Guid id);
        Task<string> AssignRoleAsync(UserRoleRequestDto request);
        Task<string> RevokeRoleAsync(Guid userId, string roleName);
    }
}