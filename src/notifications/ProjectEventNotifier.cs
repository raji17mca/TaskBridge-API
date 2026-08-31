using TaskBridge_API.Projects;

namespace TaskBridge_API.Notifications;

/// <inheritdoc cref="IProjectEventNotifier"/>
public class ProjectEventNotifier : IProjectEventNotifier
{
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ITeamMembershipRepository _teamMembershipRepository;
    private readonly ILogger<ProjectEventNotifier> _logger;

    public ProjectEventNotifier(
        IAuditService auditService,
        INotificationService notificationService,
        ITeamMembershipRepository teamMembershipRepository,
        ILogger<ProjectEventNotifier> logger)
    {
        _auditService = auditService;
        _notificationService = notificationService;
        _teamMembershipRepository = teamMembershipRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task NotifyAsync(Guid tenantId, Guid actorUserId, AuditEventType eventType, Project project, string? previousState, string? newState, CancellationToken cancellationToken = default)
    {
        var auditRequest = new RecordAuditEventRequest(eventType, "Project", project.Id.ToString(), actorUserId, tenantId, previousState, newState);
        await _auditService.RecordEventAsync(auditRequest, cancellationToken);

        var recipients = await _teamMembershipRepository.GetTeamMemberUserIdsAsync(tenantId, project.TeamId, cancellationToken);
        if (recipients.Count == 0)
        {
            _logger.LogWarning("No team members found for team {TeamId}; skipping notification dispatch for project {ProjectId}", project.TeamId, project.Id);
            return;
        }

        var message = BuildMessage(eventType, project);
        await _notificationService.DispatchAsync(tenantId, recipients, eventType, project.Id, message, cancellationToken);
    }

    private static string BuildMessage(AuditEventType eventType, Project project) => eventType switch
    {
        AuditEventType.ProjectCreated => $"Project '{project.Name}' was created.",
        AuditEventType.ProjectStatusUpdated => $"Project '{project.Name}' status changed to {project.Status}.",
        AuditEventType.ProjectDeleted => $"Project '{project.Name}' was deleted.",
        _ => $"Project '{project.Name}' changed."
    };
}
