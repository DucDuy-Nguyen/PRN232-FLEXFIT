using FlexFit.Engagement.Service.DTOs.AI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Engagement.Service.Interfaces
{
    public interface IAIClient
    {
        Task<string> GenerateContentAsync(string prompt, List<AIChatMessage>? history = null);
    }
}
