using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Services
{
    public class GymService : IGymService
    {
        private readonly IGymRepository _gymRepo;
        private readonly FlexFitDbContext _context;

        public GymService(IGymRepository gymRepo, FlexFitDbContext context)
        {
            _gymRepo = gymRepo;
            _context = context;
        }

        public async Task<IEnumerable<GymDto>> GetAllGymsAsync()
        {
            var gyms = await _gymRepo.GetAllAsync();
            return gyms.Select(g => new GymDto
            {
                GymId = g.GymId,
                OwnerId = g.OwnerId,
                GymName = g.GymName,
                Description = g.Description,
                ThumbnailUrl = g.ThumbnailUrl,
                PhoneNumber = g.PhoneNumber,
                Email = g.Email,
                Status = g.Status,
                RatingAverage = g.RatingAverage,
                TotalReviews = g.TotalReviews,
                CreatedAt = g.CreatedAt
            });
        }

        public async Task<GymDto?> GetGymByIdAsync(Guid id)
        {
            var g = await _gymRepo.GetByIdAsync(id);
            if (g == null) return null;

            return new GymDto
            {
                GymId = g.GymId,
                OwnerId = g.OwnerId,
                GymName = g.GymName,
                Description = g.Description,
                ThumbnailUrl = g.ThumbnailUrl,
                PhoneNumber = g.PhoneNumber,
                Email = g.Email,
                Status = g.Status,
                RatingAverage = g.RatingAverage,
                TotalReviews = g.TotalReviews,
                CreatedAt = g.CreatedAt
            };
        }

        public async Task<Guid> CreateGymAsync(CreateGymRequest request)
        {
            var newGym = new Gym
            {
                GymId = Guid.NewGuid(),
                OwnerId = request.OwnerId,
                GymName = request.GymName,
                Description = request.Description,
                ThumbnailUrl = request.ThumbnailUrl,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Status = "Pending",
                RatingAverage = 0,
                TotalReviews = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _gymRepo.AddAsync(newGym);

            var partnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "GymPartner");
            if (partnerRole != null)
            {
                var hasPartnerRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == request.OwnerId && ur.RoleId == partnerRole.RoleId);
                if (!hasPartnerRole)
                {
                    await _context.UserRoles.AddAsync(new UserRole { UserId = request.OwnerId, RoleId = partnerRole.RoleId, AssignedAt = DateTime.UtcNow });
                    await _context.SaveChangesAsync();
                }
            }
            return newGym.GymId;
        }

        public async Task UpdateGymAsync(Guid id, UpdateGymRequest request)
        {
            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            gym.GymName = request.GymName;
            gym.Description = request.Description;
            gym.ThumbnailUrl = request.ThumbnailUrl;
            gym.PhoneNumber = request.PhoneNumber;
            gym.Email = request.Email;
            gym.UpdatedAt = DateTime.UtcNow;

            await _gymRepo.UpdateAsync(gym);
        }

        public async Task ChangeGymStatusAsync(Guid id, string status)
        {
            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            gym.Status = status;
            gym.UpdatedAt = DateTime.UtcNow;

            await _gymRepo.UpdateAsync(gym);
        }

        public async Task DeleteGymAsync(Guid id)
        {
            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            await _gymRepo.DeleteAsync(id);
        }

        public async Task TransferGymOwnershipAsync(TransferGymOwnershipDto request)
        {
            var gym = await _gymRepo.GetByIdAsync(request.GymId);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            var newOwner = await _context.Users.FindAsync(request.NewOwnerId);
            if (newOwner == null) throw new KeyNotFoundException("Người dùng được chọn làm chủ sở hữu mới không tồn tại.");

            if (gym.OwnerId == request.NewOwnerId)
                throw new ArgumentException($"Người dùng {newOwner.FullName} đã là chủ sở hữu phòng tập này rồi.");

            Guid oldOwnerId = gym.OwnerId;
            gym.OwnerId = request.NewOwnerId;
            gym.UpdatedAt = DateTime.UtcNow;
            await _gymRepo.UpdateAsync(gym);

            var partnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "GymPartner");
            if (partnerRole != null)
            {
                var newOwnerHasRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == request.NewOwnerId && ur.RoleId == partnerRole.RoleId);
                if (!newOwnerHasRole)
                {
                    await _context.UserRoles.AddAsync(new UserRole { UserId = request.NewOwnerId, RoleId = partnerRole.RoleId, AssignedAt = DateTime.UtcNow });
                }

                var oldOwnerRemainingGyms = await _context.Gyms.CountAsync(g => g.OwnerId == oldOwnerId);
                if (oldOwnerRemainingGyms == 0)
                {
                    var oldUserRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == oldOwnerId && ur.RoleId == partnerRole.RoleId);
                    if (oldUserRole != null) _context.UserRoles.Remove(oldUserRole);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}