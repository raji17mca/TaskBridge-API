using System.ComponentModel.DataAnnotations;

namespace TaskBridge_API.Projects;

/// <summary>Request contract for creating a new project.</summary>
public record CreateProjectRequest(
    [Required] Guid TeamId,
    [Required, MaxLength(200)] string Name,
    [MaxLength(2000)] string? Description);
