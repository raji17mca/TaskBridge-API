namespace TaskBridge_API.Projects;

public class Project
{
    public int Id { get; set; }

    // Tenant/team scoping enforces multi-tenant data isolation at the query level.
    public Guid TenantId { get; set; }
    public Guid TeamId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
