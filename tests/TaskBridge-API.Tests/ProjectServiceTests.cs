using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge_API.Data;
using TaskBridge_API.Notifications;
using TaskBridge_API.Projects;
using Xunit;

namespace TaskBridge_API.Tests;

public class ProjectServiceTests
{
    private static ProjectService CreateService()
    {
        var dbContext = new TaskBridgeDbContext(
            new DbContextOptionsBuilder<TaskBridgeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var notifier = new ProjectEventNotifier(
            new AuditService(new AuditRepository(dbContext), NullLogger<AuditService>.Instance),
            new NotificationService(new NotificationRepository(dbContext), NullLogger<NotificationService>.Instance),
            new TeamMembershipRepository(dbContext),
            NullLogger<ProjectEventNotifier>.Instance);

        return new ProjectService(new ProjectRepository(dbContext), notifier, NullLogger<ProjectService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_EmptyTenantId_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(Guid.Empty, Guid.NewGuid(), new CreateProjectRequest(Guid.NewGuid(), "Name", null), default));
    }

    [Fact]
    public async Task CreateAsync_EmptyTeamId_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateProjectRequest(Guid.Empty, "Name", null), default));
    }

    [Fact]
    public async Task CreateAsync_BlankName_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateProjectRequest(Guid.NewGuid(), "   ", null), default));
    }

    [Fact]
    public async Task GetByTeamAsync_EmptyTenantId_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetByTeamAsync(Guid.Empty, Guid.NewGuid(), default));
    }
}
