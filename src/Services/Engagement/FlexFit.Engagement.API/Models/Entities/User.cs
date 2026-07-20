namespace FlexFit.Engagement.API.Models.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}
