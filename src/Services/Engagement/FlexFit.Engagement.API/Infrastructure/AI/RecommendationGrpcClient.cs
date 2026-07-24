using FlexFit.Engagement.Service.Interfaces;
using FlexFit.Recommendation.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Engagement.API.Infrastructure.AI
{
    public class RecommendationGrpcClient : IRecommendationClient
    {
        private readonly RecommendationService.RecommendationServiceClient _recommendationClient;

        public RecommendationGrpcClient(RecommendationService.RecommendationServiceClient recommendationClient)
        {
            _recommendationClient = recommendationClient;
        }

        public async Task<List<string>> GetWorkoutRecommendationsAsync(Guid userId)
        {
            var grpcRequest = new RecommendationRequest { UserId = userId.ToString() };
            var grpcResponse = await _recommendationClient.GetWorkoutRecommendationsAsync(grpcRequest);
            return grpcResponse.Recommendations.ToList();
        }

        public async Task<List<string>> GetClassRecommendationsAsync(Guid userId)
        {
            var grpcRequest = new RecommendationRequest { UserId = userId.ToString() };
            var grpcResponse = await _recommendationClient.GetClassRecommendationsAsync(grpcRequest);
            return grpcResponse.Recommendations.ToList();
        }
    }
}
