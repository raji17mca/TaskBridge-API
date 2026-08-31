namespace TaskBridge_API.Notifications;

/// <summary>Data-access abstraction for <see cref="AuditEntry"/> records. Deliberately has no Update or Remove method.</summary>
public interface IAuditRepository
{
    /// <summary>Stages a new, immutable audit entry; call <see cref="SaveChangesAsync"/> to persist it.</summary>
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Lists audit entries for an entity within a tenant, optionally filtered by date range and event type.</summary>
    Task<IReadOnlyList<AuditEntry>> GetByProjectAsync(Guid tenantId, string entityId, DateTime? from, DateTime? to, AuditEventType? eventType, CancellationToken cancellationToken = default);

    /// <summary>Persists all staged changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
