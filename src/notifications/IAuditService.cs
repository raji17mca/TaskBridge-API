namespace TaskBridge_API.Notifications;

/// <summary>Business logic for recording and querying audit history. Every method enforces tenant isolation.</summary>
public interface IAuditService
{
    /// <summary>Records an immutable audit entry for an entity state change.</summary>
    Task<AuditEntryResponse> RecordEventAsync(RecordAuditEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets audit history for a project, optionally filtered by date range and event type.</summary>
    Task<IReadOnlyList<AuditEntryResponse>> GetHistoryAsync(Guid tenantId, int projectId, DateTime? from, DateTime? to, AuditEventType? eventType, CancellationToken cancellationToken = default);
}
