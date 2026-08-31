namespace TaskBridge_API.Notifications;

// Minimal placeholder for team membership until a dedicated Team Service/model exists.
public class TeamMembership
{
    public int Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
}
