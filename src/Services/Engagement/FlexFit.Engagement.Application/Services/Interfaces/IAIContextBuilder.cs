using FlexFit.Engagement.Application.DTOs.AI;

namespace FlexFit.Engagement.Application.Services.Interfaces;

public interface IAIContextBuilder
{
    Task<AIUserContextDto> GetUserContextAsync(Guid userId);
}
