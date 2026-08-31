using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge_API.Common;

namespace TaskBridge_API.Notifications;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _userProvider;

    public NotificationsController(INotificationService notificationService, ICurrentTenantProvider tenantProvider, ICurrentUserProvider userProvider)
    {
        _notificationService = notificationService;
        _tenantProvider = tenantProvider;
        _userProvider = userProvider;
    }

    /// <summary>Gets all unread notifications for a user. Callers may only query their own notifications.</summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetUnread(Guid userId, CancellationToken cancellationToken)
    {
        if (userId != _userProvider.UserId)
        {
            // Never let a user read another user's notifications, even within the same tenant.
            return Forbid();
        }

        var notifications = await _notificationService.GetUnreadForUserAsync(_tenantProvider.TenantId, userId, cancellationToken);
        return Ok(notifications);
    }

    /// <summary>Marks a notification as read. Callers may only mark their own notifications.</summary>
    [HttpPatch("{id}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var updated = await _notificationService.MarkAsReadAsync(_tenantProvider.TenantId, _userProvider.UserId, id, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}
