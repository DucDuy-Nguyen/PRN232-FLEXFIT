using Flexfit.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IFavoriteClassService
    {
        Task<string> ToggleFavoriteClassAsync(Guid userId, Guid classId);
        Task<IEnumerable<FavoriteClassResponse>> GetMyFavoriteClassesAsync(Guid userId);
    }
}