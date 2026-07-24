using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Engagement.Service.Interfaces
{
    public interface IRecommendationClient
    {
        Task<List<string>> GetWorkoutRecommendationsAsync(Guid userId);
        Task<List<string>> GetClassRecommendationsAsync(Guid userId);
    }
}
