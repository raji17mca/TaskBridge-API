using Microsoft.AspNetCore.Mvc;
using TaskBridge_API.Projects;
using Xunit;

namespace TaskBridge_API.Tests;

public class ProjectsControllerTests
{
    [Fact]
    public void Create_ThenGetById_ReturnsSameProject()
    {
        var controller = new ProjectsController();
        var project = new Project { Name = "Sample Project" };

        var createResult = controller.Create(project);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdProject = Assert.IsType<Project>(created.Value);

        var getResult = controller.GetById(createdProject.Id);
        var ok = Assert.IsType<OkObjectResult>(getResult.Result);
        var fetched = Assert.IsType<Project>(ok.Value);

        Assert.Equal(createdProject.Id, fetched.Id);
        Assert.Equal("Sample Project", fetched.Name);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNotFound()
    {
        var controller = new ProjectsController();

        var result = controller.GetById(-1);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
