using FlexFit.Engagement.API.Models.DTOs.AI;

namespace FlexFit.Engagement.API.Services.Interfaces;

public interface IAIContextBuilder
{
    Task<AIUserContextDto> GetUserContextAsync(Guid userId);
}
