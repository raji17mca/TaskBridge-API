using System.ComponentModel.DataAnnotations;

namespace TaskBridge_API.Notifications;

/// <summary>Request contract for recording an audit event (used by the internal POST /audit endpoint).</summary>
public record RecordAuditEventRequest(
    AuditEventType EventType,
    [property: Required, MaxLength(100)] string EntityType,
    [property: Required, MaxLength(100)] string EntityId,
    [property: Required] Guid ActorUserId,
    [property: Required] Guid OrganisationId,
    string? PreviousState,
    string? NewState);
