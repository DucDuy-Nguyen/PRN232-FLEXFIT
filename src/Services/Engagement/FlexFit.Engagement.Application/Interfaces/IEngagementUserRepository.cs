namespace FlexFit.Engagement.Application.Interfaces;

/// <summary>
/// Minimal user projection needed by Engagement service.
/// Only fetches UserId — avoids coupling to full User entity.
/// </summary>
public interface IEngagementUserRepository
{
    Task<IEnumerable<Guid>> GetAllUserIdsAsync();
}
