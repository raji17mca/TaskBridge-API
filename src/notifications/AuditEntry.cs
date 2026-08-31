namespace TaskBridge_API.Notifications;

/// <summary>An immutable record of a state change to an entity. No update or delete path exists by design.</summary>
public class AuditEntry
{
    public int Id { get; init; }
    public AuditEventType EventType { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public Guid OrganisationId { get; init; }
    public string? PreviousState { get; init; }
    public string? NewState { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
