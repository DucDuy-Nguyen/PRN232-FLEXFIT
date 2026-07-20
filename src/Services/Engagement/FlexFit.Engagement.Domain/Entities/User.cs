namespace FlexFit.Engagement.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}
