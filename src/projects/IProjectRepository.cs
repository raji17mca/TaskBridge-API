namespace TaskBridge_API.Projects;

/// <summary>Data-access abstraction for <see cref="Project"/> entities. Contains no business rules.</summary>
public interface IProjectRepository
{
    /// <summary>Stages a new project for insertion; call <see cref="SaveChangesAsync"/> to persist it.</summary>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>Looks up a project scoped to a tenant. Returns null if it doesn't exist or belongs to another tenant.</summary>
    Task<Project?> GetByIdAsync(Guid tenantId, int projectId, CancellationToken cancellationToken = default);

    /// <summary>Lists all projects for a tenant/team, newest first.</summary>
    Task<IReadOnlyList<Project>> GetByTeamAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>Stages a project for deletion; call <see cref="SaveChangesAsync"/> to persist it.</summary>
    void Remove(Project project);

    /// <summary>Persists all staged changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
