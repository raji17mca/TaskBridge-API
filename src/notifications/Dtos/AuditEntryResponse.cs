namespace TaskBridge_API.Notifications;

/// <summary>Public response contract for an audit entry. Deliberately excludes OrganisationId (internal-only).</summary>
public record AuditEntryResponse(
    int Id,
    AuditEventType EventType,
    string EntityType,
    string EntityId,
    Guid ActorUserId,
    string? PreviousState,
    string? NewState,
    DateTime CreatedAt)
{
    /// <summary>Maps an <see cref="AuditEntry"/> entity to its public response contract.</summary>
    public static AuditEntryResponse FromEntity(AuditEntry entry) => new(
        entry.Id,
        entry.EventType,
        entry.EntityType,
        entry.EntityId,
        entry.ActorUserId,
        entry.PreviousState,
        entry.NewState,
        entry.CreatedAt);
}
