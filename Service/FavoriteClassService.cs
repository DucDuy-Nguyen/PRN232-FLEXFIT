using System.Linq;
using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class FavoriteClassService : IFavoriteClassService
    {
        private readonly IFavoriteClassRepository _favoriteRepo;

        public FavoriteClassService(IFavoriteClassRepository favoriteRepo)
        {
            _favoriteRepo = favoriteRepo;
        }

        public async Task<string> ToggleFavoriteClassAsync(Guid userId, Guid classId)
        {
            var existingFavorite = await _favoriteRepo.GetAsync(userId, classId);

            if (existingFavorite != null)
            {
                _favoriteRepo.Remove(existingFavorite);
                await _favoriteRepo.SaveChangesAsync();
                return "Đã xóa lớp học khỏi danh sách yêu thích.";
            }
            else
            {
                var newFavorite = new FavoriteClass
                {
                    UserId = userId,
                    ClassId = classId,
                    CreatedAt = DateTime.UtcNow
                };

                await _favoriteRepo.AddAsync(newFavorite);
                await _favoriteRepo.SaveChangesAsync();
                return "Đã thêm lớp học vào danh sách yêu thích.";
            }
        }

        public async Task<IEnumerable<FavoriteClassResponse>> GetMyFavoriteClassesAsync(Guid userId)
        {
            var favorites = await _favoriteRepo.GetByUserIdAsync(userId);

            return favorites.Select(f => new FavoriteClassResponse
            {
                ClassId = f.ClassId,
                ClassName = f.Class.ClassName,
                CoachName = f.Class.CoachName,
                ThumbnailUrl = f.Class.ThumbnailUrl,
                CreditCost = f.Class.CreditCost,
                LikedAt = f.CreatedAt
            });
        }
    }
}