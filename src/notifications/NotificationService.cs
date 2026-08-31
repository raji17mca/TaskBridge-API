namespace TaskBridge_API.Notifications;

/// <inheritdoc cref="INotificationService"/>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(Guid tenantId, IReadOnlyCollection<Guid> recipientUserIds, AuditEventType eventType, int projectId, string message, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(recipientUserIds);

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var notifications = recipientUserIds
            .Distinct()
            .Select(userId => new Notification
            {
                TenantId = tenantId,
                RecipientUserId = userId,
                EventType = eventType,
                ProjectId = projectId,
                Message = message,
                IsRead = false,
                CreatedAt = now
            })
            .ToList();

        await _repository.AddRangeAsync(notifications, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dispatched {Count} notifications for project {ProjectId} event {EventType}", notifications.Count, projectId, eventType);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationResponse>> GetUnreadForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var notifications = await _repository.GetUnreadByUserAsync(tenantId, userId, cancellationToken);
        return notifications.Select(NotificationResponse.FromEntity).ToList();
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse?> MarkAsReadAsync(Guid tenantId, Guid userId, int notificationId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        var notification = await _repository.GetByIdAsync(tenantId, notificationId, cancellationToken);
        if (notification is null || notification.RecipientUserId != userId)
        {
            // Same 404-for-everything rule as the Project Service: never reveal another user's notification exists.
            _logger.LogWarning("Notification {NotificationId} not found for tenant {TenantId} user {UserId}", notificationId, tenantId, userId);
            return null;
        }

        notification.IsRead = true;
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {NotificationId} marked read by user {UserId}", notificationId, userId);
        return NotificationResponse.FromEntity(notification);
    }
}
