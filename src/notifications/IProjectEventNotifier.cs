using TaskBridge_API.Projects;

namespace TaskBridge_API.Notifications;

/// <summary>Coordinates audit logging and notification dispatch whenever a project's state changes. This is the integration seam between the Project Service and the Notification &amp; Audit Service.</summary>
public interface IProjectEventNotifier
{
    /// <summary>Records an audit entry and notifies the project's team members of a state change.</summary>
    Task NotifyAsync(Guid tenantId, Guid actorUserId, AuditEventType eventType, Project project, string? previousState, string? newState, CancellationToken cancellationToken = default);
}
