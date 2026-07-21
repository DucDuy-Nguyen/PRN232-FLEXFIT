using FlexFit.Engagement.Service.DTOs.AI;

namespace FlexFit.Engagement.Service.Interfaces;

public interface IAIService
{
    Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId);
    Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId);
    Task<string> ChatWithAIAsync(Guid userId, AIChatRequest request);
}

