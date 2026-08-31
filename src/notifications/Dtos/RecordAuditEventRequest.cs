using System.ComponentModel.DataAnnotations;

namespace TaskBridge_API.Notifications;

/// <summary>Request contract for recording an audit event (used by the internal POST /audit endpoint).</summary>
public record RecordAuditEventRequest(
    AuditEventType EventType,
    [Required, MaxLength(100)] string EntityType,
    [Required, MaxLength(100)] string EntityId,
    [Required] Guid ActorUserId,
    [Required] Guid OrganisationId,
    string? PreviousState,
    string? NewState);
