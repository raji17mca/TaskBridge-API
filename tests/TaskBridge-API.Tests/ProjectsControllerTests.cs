using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge_API.Common;
using TaskBridge_API.Data;
using TaskBridge_API.Notifications;
using TaskBridge_API.Projects;
using Xunit;

namespace TaskBridge_API.Tests;

public class ProjectsControllerTests
{
    private sealed class FakeTenantProvider : ICurrentTenantProvider
    {
        public FakeTenantProvider(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; }
    }

    private sealed class FakeUserProvider : ICurrentUserProvider
    {
        public FakeUserProvider(Guid userId) => UserId = userId;
        public Guid UserId { get; }
    }

    // databaseName lets tests share one in-memory database across multiple "tenants" to verify isolation.
    private static ProjectsController CreateController(Guid tenantId, string? databaseName = null, Guid actorUserId = default)
    {
        var dbContext = new TaskBridgeDbContext(
            new DbContextOptionsBuilder<TaskBridgeDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options);

        var notifier = new ProjectEventNotifier(
            new AuditService(new AuditRepository(dbContext), NullLogger<AuditService>.Instance),
            new NotificationService(new NotificationRepository(dbContext), NullLogger<NotificationService>.Instance),
            new TeamMembershipRepository(dbContext),
            NullLogger<ProjectEventNotifier>.Instance);

        var service = new ProjectService(new ProjectRepository(dbContext), notifier, NullLogger<ProjectService>.Instance);
        var actor = actorUserId == default ? Guid.NewGuid() : actorUserId;
        return new ProjectsController(service, new FakeTenantProvider(tenantId), new FakeUserProvider(actor));
    }

    [Fact]
    public async Task Create_ThenGetByTeam_ReturnsProject()
    {
        var teamId = Guid.NewGuid();
        var controller = CreateController(Guid.NewGuid());

        var createResult = await controller.Create(new CreateProjectRequest(teamId, "Sample Project", null), default);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdProject = Assert.IsType<ProjectResponse>(created.Value);

        var getResult = await controller.GetByTeam(teamId, default);
        var ok = Assert.IsType<OkObjectResult>(getResult.Result);
        var projects = Assert.IsAssignableFrom<IEnumerable<ProjectResponse>>(ok.Value);

        Assert.Contains(projects, p => p.Id == createdProject.Id && p.Name == "Sample Project");
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController(Guid.NewGuid());

        var result = await controller.UpdateStatus(-1, new UpdateProjectStatusRequest(ProjectStatus.InProgress), default);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ExistingProject_RemovesIt()
    {
        var teamId = Guid.NewGuid();
        var controller = CreateController(Guid.NewGuid());
        var createResult = await controller.Create(new CreateProjectRequest(teamId, "To Delete", null), default);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdProject = Assert.IsType<ProjectResponse>(created.Value);

        var deleteResult = await controller.Delete(createdProject.Id, default);

        Assert.IsType<NoContentResult>(deleteResult);
    }

    [Fact]
    public async Task GetByTeam_DifferentTenant_CannotSeeOtherTenantsProjects()
    {
        var sharedDatabase = Guid.NewGuid().ToString();
        var teamId = Guid.NewGuid();

        var ownerController = CreateController(Guid.NewGuid(), sharedDatabase);
        await ownerController.Create(new CreateProjectRequest(teamId, "Owner Project", null), default);

        var otherTenantController = CreateController(Guid.NewGuid(), sharedDatabase);
        var result = await otherTenantController.GetByTeam(teamId, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var projects = Assert.IsAssignableFrom<IEnumerable<ProjectResponse>>(ok.Value);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task UpdateStatus_ProjectBelongsToDifferentTenant_ReturnsNotFound()
    {
        var sharedDatabase = Guid.NewGuid().ToString();
        var teamId = Guid.NewGuid();

        var ownerController = CreateController(Guid.NewGuid(), sharedDatabase);
        var createResult = await ownerController.Create(new CreateProjectRequest(teamId, "Owner Project", null), default);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdProject = Assert.IsType<ProjectResponse>(created.Value);

        // Different tenant, same project id - must get 404, never a 403 that would confirm the id's existence.
        var attackerController = CreateController(Guid.NewGuid(), sharedDatabase);
        var updateResult = await attackerController.UpdateStatus(createdProject.Id, new UpdateProjectStatusRequest(ProjectStatus.Completed), default);

        Assert.IsType<NotFoundResult>(updateResult.Result);
    }
}
