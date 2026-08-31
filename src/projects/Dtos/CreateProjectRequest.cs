using System.ComponentModel.DataAnnotations;

namespace TaskBridge_API.Projects;

/// <summary>Request contract for creating a new project.</summary>
public record CreateProjectRequest(
    [property: Required] Guid TeamId,
    [property: Required, MaxLength(200)] string Name,
    [property: MaxLength(2000)] string? Description);
