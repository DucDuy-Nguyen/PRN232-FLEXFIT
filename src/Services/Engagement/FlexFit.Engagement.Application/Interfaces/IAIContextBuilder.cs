using FlexFit.Engagement.Application.DTOs.AI;

namespace FlexFit.Engagement.Application.Interfaces;

public interface IAIContextBuilder
{
    Task<AIUserContextDto> BuildUserContextAsync(Guid userId);
}
