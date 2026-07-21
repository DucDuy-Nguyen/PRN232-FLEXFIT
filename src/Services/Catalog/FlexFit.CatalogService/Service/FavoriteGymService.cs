using FlexFit.CatalogService.DTOs;
using FlexFit.CatalogService.Models;
using FlexFit.CatalogService.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Service;

public class FavoriteGymService : IFavoriteGymService
{
    private readonly IFavoriteGymRepository _favoriteRepo;

    public FavoriteGymService(IFavoriteGymRepository favoriteRepo)
    {
        _favoriteRepo = favoriteRepo;
    }

    public async Task<string> ToggleFavoriteGymAsync(Guid userId, Guid gymId)
    {
        var existingFavorite = await _favoriteRepo.GetAsync(userId, gymId);

        if (existingFavorite != null)
        {
            _favoriteRepo.Remove(existingFavorite);
            await _favoriteRepo.SaveChangesAsync();
            return "Đã xóa phòng gym khỏi danh sách yêu thích.";
        }
        else
        {
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
