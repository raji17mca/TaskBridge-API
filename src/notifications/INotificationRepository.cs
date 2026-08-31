namespace TaskBridge_API.Notifications;

/// <summary>Data-access abstraction for <see cref="Notification"/> records.</summary>
public interface INotificationRepository
{
    /// <summary>Stages notification records for insertion; call <see cref="SaveChangesAsync"/> to persist them.</summary>
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

    /// <summary>Lists all unread notifications for a user within a tenant, newest first.</summary>
    Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Looks up a notification scoped to a tenant. Returns null if it doesn't exist or belongs to another tenant.</summary>
    Task<Notification?> GetByIdAsync(Guid tenantId, int notificationId, CancellationToken cancellationToken = default);

    /// <summary>Persists all staged changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
