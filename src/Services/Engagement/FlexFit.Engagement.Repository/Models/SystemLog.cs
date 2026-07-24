namespace FlexFit.Engagement.Repository.Models;

public class SystemLog
{
    public Guid LogId { get; set; }
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual User? User { get; set; }
}

