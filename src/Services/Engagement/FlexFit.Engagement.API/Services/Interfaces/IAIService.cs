using FlexFit.Engagement.API.Models.DTOs.AI;

namespace FlexFit.Engagement.API.Services.Interfaces;

public interface IAIService
{
    Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId);
    Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId);
    Task<string> ChatWithAIAsync(Guid userId, AIChatRequest request);
}
