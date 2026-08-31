using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge_API.Common;
using TaskBridge_API.Data;
using TaskBridge_API.Notifications;
using TaskBridge_API.Projects;
using Xunit;

namespace TaskBridge_API.Tests;

public class NotificationsControllerTests
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

    private static (NotificationsController Controller, ProjectService ProjectService, Guid TenantId, Guid UserId, Guid TeamId) CreateController()
    {
        var dbContext = new TaskBridgeDbContext(
            new DbContextOptionsBuilder<TaskBridgeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        dbContext.TeamMemberships.Add(new TeamMembership { TenantId = tenantId, TeamId = teamId, UserId = userId });
        dbContext.SaveChanges();

        var auditService = new AuditService(new AuditRepository(dbContext), NullLogger<AuditService>.Instance);
        var notificationService = new NotificationService(new NotificationRepository(dbContext), NullLogger<NotificationService>.Instance);
        var notifier = new ProjectEventNotifier(auditService, notificationService, new TeamMembershipRepository(dbContext), NullLogger<ProjectEventNotifier>.Instance);
        var projectService = new ProjectService(new ProjectRepository(dbContext), notifier, NullLogger<ProjectService>.Instance);

        var controller = new NotificationsController(notificationService, new FakeTenantProvider(tenantId), new FakeUserProvider(userId));
        return (controller, projectService, tenantId, userId, teamId);
    }

    [Fact]
    public async Task GetUnread_ThenMarkAsRead_SetsIsReadTrue()
    {
        var (controller, projectService, tenantId, userId, teamId) = CreateController();
        await projectService.CreateAsync(tenantId, userId, new CreateProjectRequest(teamId, "Notify Test", null), default);

        var unreadResult = await controller.GetUnread(userId, default);
        var ok = Assert.IsType<OkObjectResult>(unreadResult.Result);
        var notifications = Assert.IsAssignableFrom<IEnumerable<NotificationResponse>>(ok.Value).ToList();
        Assert.Single(notifications);

        var markResult = await controller.MarkAsRead(notifications[0].Id, default);
        var marked = Assert.IsType<OkObjectResult>(markResult.Result);
        var updated = Assert.IsType<NotificationResponse>(marked.Value);
        Assert.True(updated.IsRead);

        var afterResult = await controller.GetUnread(userId, default);
        var afterOk = Assert.IsType<OkObjectResult>(afterResult.Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<NotificationResponse>>(afterOk.Value));
    }

    [Fact]
    public async Task MarkAsRead_UnknownId_ReturnsNotFound()
    {
        var (controller, _, _, _, _) = CreateController();

        var result = await controller.MarkAsRead(-1, default);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetUnread_AnotherUsersId_ReturnsForbidden()
    {
        var (controller, _, _, _, _) = CreateController();

        var result = await controller.GetUnread(Guid.NewGuid(), default);

        Assert.IsType<ForbidResult>(result.Result);
    }
}
