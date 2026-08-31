namespace TaskBridge_API.Projects;

/// <summary>Business logic for managing projects. Every method enforces tenant isolation.</summary>
public interface IProjectService
{
    /// <summary>Creates a new project owned by the given tenant/team.</summary>
    /// <param name="tenantId">The caller's tenant, resolved from their authenticated identity.</param>
    /// <param name="actorUserId">The user performing the action, recorded on the resulting audit entry.</param>
    /// <param name="request">The project name/description/team to create under.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The newly created project.</returns>
    /// <exception cref="ArgumentException"><paramref name="tenantId"/> is empty, or <paramref name="request"/> has an empty team id or a blank name.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    Task<ProjectResponse> CreateAsync(Guid tenantId, Guid actorUserId, CreateProjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a project's status.</summary>
    /// <param name="tenantId">The caller's tenant; the project must belong to this tenant.</param>
    /// <param name="actorUserId">The user performing the action, recorded on the resulting audit entry.</param>
    /// <param name="projectId">The id of the project to update.</param>
    /// <param name="request">The new status to apply.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated project, or null if no project with <paramref name="projectId"/> exists for the tenant (including if it belongs to a different tenant).</returns>
    /// <exception cref="ArgumentException"><paramref name="tenantId"/> is empty, or <paramref name="request"/>.Status is not a defined <see cref="ProjectStatus"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    Task<ProjectResponse?> UpdateStatusAsync(Guid tenantId, Guid actorUserId, int projectId, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists all projects owned by the given tenant/team, newest first.</summary>
    /// <param name="tenantId">The caller's tenant.</param>
    /// <param name="teamId">The team whose projects should be listed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The team's projects for that tenant, ordered by creation date descending. Empty if none exist.</returns>
    /// <exception cref="ArgumentException"><paramref name="tenantId"/> or <paramref name="teamId"/> is empty.</exception>
    Task<IReadOnlyList<ProjectResponse>> GetByTeamAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a project.</summary>
    /// <param name="tenantId">The caller's tenant; the project must belong to this tenant.</param>
    /// <param name="actorUserId">The user performing the action, recorded on the resulting audit entry.</param>
    /// <param name="projectId">The id of the project to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>True if the project was found and deleted; false if no project with <paramref name="projectId"/> exists for the tenant (including if it belongs to a different tenant).</returns>
    /// <exception cref="ArgumentException"><paramref name="tenantId"/> is empty.</exception>
    Task<bool> DeleteAsync(Guid tenantId, Guid actorUserId, int projectId, CancellationToken cancellationToken = default);
}
