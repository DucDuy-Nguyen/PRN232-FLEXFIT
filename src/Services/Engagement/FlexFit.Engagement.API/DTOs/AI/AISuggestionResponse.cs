namespace FlexFit.Engagement.API.DTOs.AI;

public class AISuggestionResponse
{
    public string Suggestion { get; set; } = string.Empty;
    public DateTime SuggestedAt { get; set; } = DateTime.UtcNow;
}
