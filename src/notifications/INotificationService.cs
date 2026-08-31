namespace TaskBridge_API.Notifications;

/// <summary>Business logic for dispatching and querying notifications. Every method enforces tenant isolation.</summary>
public interface INotificationService
{
    /// <summary>Creates a notification record for each recipient about a project event.</summary>
    Task DispatchAsync(Guid tenantId, IReadOnlyCollection<Guid> recipientUserIds, AuditEventType eventType, int projectId, string message, CancellationToken cancellationToken = default);

    /// <summary>Lists all unread notifications for a user, newest first.</summary>
    Task<IReadOnlyList<NotificationResponse>> GetUnreadForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Marks a notification as read.</summary>
    /// <returns>The updated notification, or null if it doesn't exist for the given tenant/user.</returns>
    Task<NotificationResponse?> MarkAsReadAsync(Guid tenantId, Guid userId, int notificationId, CancellationToken cancellationToken = default);
}
