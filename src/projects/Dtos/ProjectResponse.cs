namespace TaskBridge_API.Projects;

/// <summary>Public response contract for a project; deliberately excludes internal-only fields (e.g. TenantId).</summary>
public record ProjectResponse(
    int Id,
    Guid TeamId,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    /// <summary>Maps a <see cref="Project"/> entity to its public response contract.</summary>
    public static ProjectResponse FromEntity(Project project) => new(
        project.Id,
        project.TeamId,
        project.Name,
        project.Description,
        project.Status,
        project.CreatedAt,
        project.UpdatedAt);
}
