using FlexFit.Catalog.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Interfaces;

public interface IFavoriteClassService
{
    Task<string> ToggleFavoriteClassAsync(Guid userId, Guid classId);
    Task<IEnumerable<FavoriteClassResponse>> GetMyFavoriteClassesAsync(Guid userId);
}


