using System;
using System.Threading.Tasks;
using Flexfit.DTOs.AI;

namespace Flexfit.Service.AI;

public interface IAIContextBuilder
{
    Task<AIUserContextDto> BuildUserContextAsync(Guid userId);
}
