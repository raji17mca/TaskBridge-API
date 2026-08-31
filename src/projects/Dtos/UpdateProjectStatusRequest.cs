namespace TaskBridge_API.Projects;

/// <summary>Request contract for transitioning a project's status.</summary>
public record UpdateProjectStatusRequest(ProjectStatus Status);
