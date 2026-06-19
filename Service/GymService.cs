using Flexfit.DTOs;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Services
{
    public class GymService : IGymService
    {
        private readonly IGymRepository _gymRepo;

        public GymService(IGymRepository gymRepo)
        {
            _gymRepo = gymRepo;
        }

        private GymDto MapToDto(Gym g)
        {
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

        public async Task<Guid> CreateGymAsync(CreateGymRequest request, Guid currentUserId)
        {
            // 1. 🛑 CHECK: Đảm bảo người dùng được chỉ định làm Owner tồn tại trong hệ thống
            var ownerUser = await _gymRepo.GetUserByIdAsync(request.OwnerId);
            if (ownerUser == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản Người nhận quyền sở hữu (OwnerId).");
            }

            // 2. Khởi tạo đối tượng Gym mới gán cho OwnerId từ request
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
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _gymRepo.AddAsync(newGym);

            // 3. 🔑 XỬ LÝ CHUYỂN ĐỔI QUYỀN LỰC: XÓA ROLE CŨ & THÊM ROLE GYMPARTNER
            var partnerRole = await _gymRepo.GetRoleByNameAsync("GymPartner");
            if (partnerRole != null)
            {
                // Bước A: Tìm và xóa Role cũ (Ví dụ ở đây là role "Member")
                var memberRole = await _gymRepo.GetRoleByNameAsync("Member");
                if (memberRole != null)
                {
                    var hasMemberRole = await _gymRepo.UserHasRoleAsync(request.OwnerId, memberRole.RoleId);
                    if (hasMemberRole)
                    {
                        // Xóa liên kết role cũ của user này
                        await _gymRepo.RemoveUserRoleAsync(request.OwnerId, memberRole.RoleId);
                    }
                }

                // Bước B: Cấp quyền GymPartner mới cho Owner nếu chưa có
                var hasPartnerRole = await _gymRepo.UserHasRoleAsync(request.OwnerId, partnerRole.RoleId);
                if (!hasPartnerRole)
                {
                    await _gymRepo.AddUserRoleAsync(new UserRole
                    {
                        UserId = request.OwnerId,
                        RoleId = partnerRole.RoleId,
                        AssignedAt = DateTimeHelper.GetVietnamTime()
                    });
                }
            }

            // Lưu toàn bộ thay đổi (Tạo phòng tập, xóa role cũ, thêm role mới)
            await _gymRepo.SaveChangesAsync();

            return newGym.GymId;
        }

        public async Task UpdateGymAsync(Guid id, UpdateGymRequest request, Guid currentUserId)
        {
            // 🛑 CHECK quyền sở hữu
            var isOwner = await _gymRepo.CheckGymOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này.");

            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            gym.GymName = request.GymName;
            gym.Description = request.Description;
            gym.ThumbnailUrl = request.ThumbnailUrl;
            gym.PhoneNumber = request.PhoneNumber;
            gym.Email = request.Email;
            gym.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _gymRepo.UpdateAsync(gym);
        }

        public async Task ChangeGymStatusAsync(Guid id, string status, Guid currentUserId, bool isAdmin = false)
        {
            if (!isAdmin)
            {
                // 🛑 CHECK quyền sở hữu
                var isOwner = await _gymRepo.CheckGymOwnershipAsync(id, currentUserId);
                if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này.");
            }

            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            gym.Status = status;
            gym.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _gymRepo.UpdateAsync(gym);
        }

        public async Task DeleteGymAsync(Guid id, Guid currentUserId)
        {
            // 🛑 CHECK quyền sở hữu
            var isOwner = await _gymRepo.CheckGymOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này.");

            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            await _gymRepo.DeleteAsync(id);
        }

        public async Task TransferGymOwnershipAsync(TransferGymOwnershipDto request, Guid currentUserId)
        {
            // 🛑 CHECK quyền sở hữu (Phải là chủ hiện tại mới được sang tên)
            var isOwner = await _gymRepo.CheckGymOwnershipAsync(request.GymId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu của phòng tập này để thực hiện chuyển nhượng.");

            var gym = await _gymRepo.GetByIdAsync(request.GymId);
            if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            var newOwner = await _gymRepo.GetUserByIdAsync(request.NewOwnerId);
            if (newOwner == null) throw new KeyNotFoundException("Người dùng được chọn làm chủ sở hữu mới không tồn tại.");

            if (gym.OwnerId == request.NewOwnerId)
                throw new ArgumentException($"Người dùng {newOwner.FullName} đã là chủ sở hữu phòng tập này rồi.");

            Guid oldOwnerId = gym.OwnerId;
            gym.OwnerId = request.NewOwnerId;
            gym.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _gymRepo.UpdateAsync(gym);

            var partnerRole = await _gymRepo.GetRoleByNameAsync("GymPartner");
            if (partnerRole != null)
            {
                var newOwnerHasRole = await _gymRepo.UserHasRoleAsync(request.NewOwnerId, partnerRole.RoleId);
                if (!newOwnerHasRole)
                {
                    await _gymRepo.AddUserRoleAsync(new UserRole
                    {
                        UserId = request.NewOwnerId,
                        RoleId = partnerRole.RoleId,
                        AssignedAt = DateTimeHelper.GetVietnamTime()
                    });
                }

                var oldOwnerRemainingGyms = await _gymRepo.CountGymsByOwnerIdAsync(oldOwnerId);
                if (oldOwnerRemainingGyms == 0)
                {
                    await _gymRepo.RemoveUserRoleAsync(oldOwnerId, partnerRole.RoleId);
                }
                await _gymRepo.SaveChangesAsync();
            }
        }
    }
}