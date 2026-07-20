using FlexFit.Engagement.Application.DTOs.AI;

namespace FlexFit.Engagement.Application.Services.Interfaces;

public interface IAIService
{
    Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId);
    Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId);
    Task<string> ChatWithAIAsync(Guid userId, AIChatRequest request);
}
