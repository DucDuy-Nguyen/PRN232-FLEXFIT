namespace FlexFit.Engagement.API.Models.DTOs.AI;

public class AIChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<AIChatMessage>? History { get; set; }
}

public class AIChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "model"
    public string Content { get; set; } = string.Empty;
}
