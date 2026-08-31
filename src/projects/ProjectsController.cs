using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge_API.Common;

namespace TaskBridge_API.Projects;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _userProvider;

    public ProjectsController(IProjectService projectService, ICurrentTenantProvider tenantProvider, ICurrentUserProvider userProvider)
    {
        _projectService = projectService;
        _tenantProvider = tenantProvider;
        _userProvider = userProvider;
    }

    /// <summary>Lists all projects for the given team within the caller's tenant.</summary>
    [HttpGet("team/{teamId}")]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetByTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetByTeamAsync(_tenantProvider.TenantId, teamId, cancellationToken);
        return Ok(projects);
    }

    /// <summary>Creates a new project owned by the caller's tenant.</summary>
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _projectService.CreateAsync(_tenantProvider.TenantId, _userProvider.UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetByTeam), new { teamId = project.TeamId }, project);
    }

    /// <summary>Updates a project's status. Returns 404 if the project doesn't exist for the caller's tenant.</summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<ProjectResponse>> UpdateStatus(int id, UpdateProjectStatusRequest request, CancellationToken cancellationToken)
    {
        var project = await _projectService.UpdateStatusAsync(_tenantProvider.TenantId, _userProvider.UserId, id, request, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    /// <summary>Deletes a project. Returns 404 if the project doesn't exist for the caller's tenant.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _projectService.DeleteAsync(_tenantProvider.TenantId, _userProvider.UserId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
