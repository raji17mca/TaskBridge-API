using Microsoft.EntityFrameworkCore;
using TaskBridge_API.Data;

namespace TaskBridge_API.Notifications;

/// <inheritdoc cref="INotificationRepository"/>
public class NotificationRepository : INotificationRepository
{
    private readonly TaskBridgeDbContext _dbContext;

    public NotificationRepository(TaskBridgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.TenantId == tenantId && n.RecipientUserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Notification?> GetByIdAsync(Guid tenantId, int notificationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.TenantId == tenantId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
