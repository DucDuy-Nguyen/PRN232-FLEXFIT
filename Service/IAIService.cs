using System;
using System.Threading.Tasks;
using Flexfit.DTOs.AI;

namespace Flexfit.Service;

public interface IAIService
{
    Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId);
    Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId);
    Task<string> ChatWithAIAsync(Guid userId, AIChatRequest request);
}
