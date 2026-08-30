using Microsoft.AspNetCore.Mvc;

namespace TaskBridge_API.Projects;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    // In-memory store; replace with a persistent data store later.
    private static readonly List<Project> Projects = new();
    private static int _nextId = 1;

    [HttpGet]
    public ActionResult<IEnumerable<Project>> GetAll() => Ok(Projects);

    [HttpGet("{id}")]
    public ActionResult<Project> GetById(int id)
    {
        var project = Projects.FirstOrDefault(p => p.Id == id);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public ActionResult<Project> Create(Project project)
    {
        project.Id = _nextId++;
        project.CreatedAt = DateTime.UtcNow;
        Projects.Add(project);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Project updated)
    {
        var project = Projects.FirstOrDefault(p => p.Id == id);
        if (project is null) return NotFound();

        project.Name = updated.Name;
        project.Description = updated.Description;
        project.IsCompleted = updated.IsCompleted;
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var project = Projects.FirstOrDefault(p => p.Id == id);
        if (project is null) return NotFound();

        Projects.Remove(project);
        return NoContent();
    }
}
