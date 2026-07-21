using FlexFit.Engagement.Service.DTOs.AI;

namespace FlexFit.Engagement.Service.Interfaces;

public interface IAIContextBuilder
{
    Task<AIUserContextDto> GetUserContextAsync(Guid userId);
}

