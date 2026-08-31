namespace TaskBridge_API.Notifications;

public class Notification
{
    public int Id { get; init; }

    // Not in the literal spec field list, but required so a user id can never be read across tenants.
    public Guid TenantId { get; init; }

    public Guid RecipientUserId { get; init; }
    public AuditEventType EventType { get; init; }
    public int ProjectId { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
