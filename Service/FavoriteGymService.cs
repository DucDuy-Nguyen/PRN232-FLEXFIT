using System.Linq; // 👈 1. THÊM DÒNG NÀY ĐỂ HẾT LỖI ĐỎ Ở HÀM SELECT
using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class FavoriteGymService : IFavoriteGymService
    {
        private readonly IFavoriteGymRepository _favoriteRepo;

        public FavoriteGymService(IFavoriteGymRepository favoriteRepo)
        {
            _favoriteRepo = favoriteRepo;
        }

        public async Task<string> ToggleFavoriteGymAsync(Guid userId, Guid gymId)
        {
            // 1. Kiểm tra xem hội viên đã thích phòng gym này chưa
            var existingFavorite = await _favoriteRepo.GetAsync(userId, gymId);

            if (existingFavorite != null)
            {
                // 2. Nếu đã thích -> Tiến hành HỦY YÊU THÍCH
                _favoriteRepo.Remove(existingFavorite);
                await _favoriteRepo.SaveChangesAsync();
                return "Đã xóa phòng gym khỏi danh sách yêu thích.";
            }
            else
            {
                // 3. Nếu chưa thích -> Tiến hành THÊM VÀO YÊU THÍCH
                var newFavorite = new FavoriteGym
                {
                    UserId = userId,
                    GymId = gymId,
                    CreatedAt = DateTime.UtcNow
                };

                await _favoriteRepo.AddAsync(newFavorite);
                await _favoriteRepo.SaveChangesAsync();
                return "Đã thêm phòng gym vào danh sách yêu thích.";
            }
        }

        public async Task<IEnumerable<FavoriteGymResponse>> GetMyFavoriteGymsAsync(Guid userId)
        {
            var favorites = await _favoriteRepo.GetByUserIdAsync(userId);

            // Thực hiện map thủ công để bẻ gãy hoàn toàn vòng lặp Object Cycle
            return favorites.Select(f => new FavoriteGymResponse
            {
                GymId = f.GymId,
                GymName = f.Gym.GymName,
                ThumbnailUrl = f.Gym.ThumbnailUrl,
                PhoneNumber = f.Gym.PhoneNumber,
                LikedAt = f.CreatedAt
            });
        }
    }
}