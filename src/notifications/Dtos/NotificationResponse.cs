namespace TaskBridge_API.Notifications;

/// <summary>Public response contract for a notification.</summary>
public record NotificationResponse(
    int Id,
    AuditEventType EventType,
    int ProjectId,
    string Message,
    bool IsRead,
    DateTime CreatedAt)
{
    /// <summary>Maps a <see cref="Notification"/> entity to its public response contract.</summary>
    public static NotificationResponse FromEntity(Notification notification) => new(
        notification.Id,
        notification.EventType,
        notification.ProjectId,
        notification.Message,
        notification.IsRead,
        notification.CreatedAt);
}
