using System.Text.Json;
using System.Text.Json.Serialization;
using TaskBridge_API.Notifications;

namespace TaskBridge_API.Projects;

/// <inheritdoc cref="IProjectService"/>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IProjectEventNotifier _projectEventNotifier;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository repository, IProjectEventNotifier projectEventNotifier, ILogger<ProjectService> logger)
    {
        _repository = repository;
        _projectEventNotifier = projectEventNotifier;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ProjectResponse> CreateAsync(Guid tenantId, Guid actorUserId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        if (request.TeamId == Guid.Empty)
        {
            throw new ArgumentException("Team id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Project name is required.", nameof(request));
        }

        var project = new Project
        {
            TenantId = tenantId,
            TeamId = request.TeamId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Status = ProjectStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(project, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} created for tenant {TenantId} team {TeamId}", project.Id, tenantId, request.TeamId);

        // Audit trail + team notifications are part of the create operation, not a fire-and-forget side effect.
        await _projectEventNotifier.NotifyAsync(tenantId, actorUserId, AuditEventType.ProjectCreated, project, previousState: null, newState: Snapshot(project), cancellationToken);

        return ProjectResponse.FromEntity(project);
    }

    /// <inheritdoc/>
    public async Task<ProjectResponse?> UpdateStatusAsync(Guid tenantId, Guid actorUserId, int projectId, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(typeof(ProjectStatus), request.Status))
        {
            throw new ArgumentException($"'{request.Status}' is not a valid project status.", nameof(request));
        }

        // Scoping by tenantId prevents callers from updating another tenant's project.
        var project = await _repository.GetByIdAsync(tenantId, projectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Project {ProjectId} not found for tenant {TenantId} during status update", projectId, tenantId);
            return null;
        }

        var previousState = Snapshot(project);
        project.Status = request.Status;
        project.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} status changed to {Status} for tenant {TenantId}", projectId, request.Status, tenantId);

        await _projectEventNotifier.NotifyAsync(tenantId, actorUserId, AuditEventType.ProjectStatusUpdated, project, previousState, Snapshot(project), cancellationToken);

        return ProjectResponse.FromEntity(project);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectResponse>> GetByTeamAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);

        if (teamId == Guid.Empty)
        {
            throw new ArgumentException("Team id is required.", nameof(teamId));
        }

        var projects = await _repository.GetByTeamAsync(tenantId, teamId, cancellationToken);
        return projects.Select(ProjectResponse.FromEntity).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid tenantId, Guid actorUserId, int projectId, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);

        var project = await _repository.GetByIdAsync(tenantId, projectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Project {ProjectId} not found for tenant {TenantId} during delete", projectId, tenantId);
            return false;
        }

        var previousState = Snapshot(project);
        _repository.Remove(project);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} deleted for tenant {TenantId}", projectId, tenantId);

        await _projectEventNotifier.NotifyAsync(tenantId, actorUserId, AuditEventType.ProjectDeleted, project, previousState, newState: null, cancellationToken);

        return true;
    }

    private static void ValidateTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }
    }

    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static string Snapshot(Project project) =>
        JsonSerializer.Serialize(new { project.TeamId, project.Name, project.Description, project.Status }, SnapshotOptions);
}
