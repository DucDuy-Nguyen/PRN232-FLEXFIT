using FlexFit.Catalog.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Interfaces;

public interface IFavoriteGymService
{
    Task<string> ToggleFavoriteGymAsync(Guid userId, Guid gymId);
    Task<IEnumerable<FavoriteGymResponse>> GetMyFavoriteGymsAsync(Guid userId);
}


