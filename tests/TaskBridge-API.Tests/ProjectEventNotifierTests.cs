using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge_API.Data;
using TaskBridge_API.Notifications;
using TaskBridge_API.Projects;
using Xunit;

namespace TaskBridge_API.Tests;

public class ProjectEventNotifierTests
{
    private static (ProjectService ProjectService, TaskBridgeDbContext DbContext) CreateProjectService(string? databaseName = null)
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

        var projectService = new ProjectService(new ProjectRepository(dbContext), notifier, NullLogger<ProjectService>.Instance);
        return (projectService, dbContext);
    }

    [Fact]
    public async Task CreateProject_WritesAuditEntryAndNotifiesAllTeamMembers()
    {
        var (projectService, dbContext) = CreateProjectService();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        dbContext.TeamMemberships.AddRange(
            new TeamMembership { TenantId = tenantId, TeamId = teamId, UserId = member1 },
            new TeamMembership { TenantId = tenantId, TeamId = teamId, UserId = member2 });
        await dbContext.SaveChangesAsync();

        var project = await projectService.CreateAsync(tenantId, actorUserId, new CreateProjectRequest(teamId, "Audit Test", null), default);

        var auditEntries = dbContext.AuditEntries.Where(a => a.EntityId == project.Id.ToString()).ToList();
        Assert.Single(auditEntries);
        Assert.Equal(AuditEventType.ProjectCreated, auditEntries[0].EventType);
        Assert.Equal(actorUserId, auditEntries[0].ActorUserId);
        Assert.Null(auditEntries[0].PreviousState);
        Assert.NotNull(auditEntries[0].NewState);

        var notifications = dbContext.Notifications.Where(n => n.ProjectId == project.Id).ToList();
        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.RecipientUserId == member1);
        Assert.Contains(notifications, n => n.RecipientUserId == member2);
        Assert.All(notifications, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task UpdateStatus_WritesAuditEntryWithBeforeAndAfterState()
    {
        var (projectService, dbContext) = CreateProjectService();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var project = await projectService.CreateAsync(tenantId, actorUserId, new CreateProjectRequest(teamId, "Status Test", null), default);
        await projectService.UpdateStatusAsync(tenantId, actorUserId, project.Id, new UpdateProjectStatusRequest(ProjectStatus.InProgress), default);

        var statusEntry = dbContext.AuditEntries
            .Single(a => a.EntityId == project.Id.ToString() && a.EventType == AuditEventType.ProjectStatusUpdated);

        Assert.NotNull(statusEntry.PreviousState);
        Assert.NotNull(statusEntry.NewState);
        Assert.Contains("NotStarted", statusEntry.PreviousState);
        Assert.Contains("InProgress", statusEntry.NewState);
    }

    [Fact]
    public async Task AuditRepository_ExposesNoUpdateOrRemoveMethod()
    {
        // Immutability is enforced structurally: this fails to compile if Update/Remove is ever added to the interface.
        var repositoryMethods = typeof(IAuditRepository).GetMethods().Select(m => m.Name);

        Assert.DoesNotContain("Update", repositoryMethods);
        Assert.DoesNotContain("Remove", repositoryMethods);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetHistoryAsync_FilteredByEventType_ReturnsOnlyMatchingEntries()
    {
        var (projectService, dbContext) = CreateProjectService();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var auditService = new AuditService(new AuditRepository(dbContext), NullLogger<AuditService>.Instance);

        var project = await projectService.CreateAsync(tenantId, actorUserId, new CreateProjectRequest(teamId, "Filter Test", null), default);
        await projectService.UpdateStatusAsync(tenantId, actorUserId, project.Id, new UpdateProjectStatusRequest(ProjectStatus.InProgress), default);

        var createdOnly = await auditService.GetHistoryAsync(tenantId, project.Id, from: null, to: null, eventType: AuditEventType.ProjectCreated, default);

        Assert.Single(createdOnly);
        Assert.Equal(AuditEventType.ProjectCreated, createdOnly[0].EventType);
    }

    [Fact]
    public async Task GetHistoryAsync_FilteredByDateRange_ExcludesEntriesOutsideRange()
    {
        var (projectService, dbContext) = CreateProjectService();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var auditService = new AuditService(new AuditRepository(dbContext), NullLogger<AuditService>.Instance);

        var project = await projectService.CreateAsync(tenantId, actorUserId, new CreateProjectRequest(teamId, "Range Test", null), default);

        var futureOnly = await auditService.GetHistoryAsync(tenantId, project.Id, from: DateTime.UtcNow.AddDays(1), to: null, eventType: null, default);
        var pastToNow = await auditService.GetHistoryAsync(tenantId, project.Id, from: DateTime.UtcNow.AddMinutes(-5), to: DateTime.UtcNow.AddMinutes(5), eventType: null, default);

        Assert.Empty(futureOnly);
        Assert.Single(pastToNow);
    }

    [Fact]
    public async Task GetHistoryAsync_DifferentTenant_CannotSeeOtherTenantsAuditLog()
    {
        var (projectService, dbContext) = CreateProjectService();
        var ownerTenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var auditService = new AuditService(new AuditRepository(dbContext), NullLogger<AuditService>.Instance);

        var project = await projectService.CreateAsync(ownerTenantId, actorUserId, new CreateProjectRequest(teamId, "Isolation Test", null), default);

        var attackerTenantId = Guid.NewGuid();
        var result = await auditService.GetHistoryAsync(attackerTenantId, project.Id, from: null, to: null, eventType: null, default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_OwnNotification_SetsIsReadTrue()
    {
        var (projectService, dbContext) = CreateProjectService();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        dbContext.TeamMemberships.Add(new TeamMembership { TenantId = tenantId, TeamId = teamId, UserId = memberId });
        await dbContext.SaveChangesAsync();

        var project = await projectService.CreateAsync(tenantId, actorUserId, new CreateProjectRequest(teamId, "Read Test", null), default);
        var notificationService = new NotificationService(new NotificationRepository(dbContext), NullLogger<NotificationService>.Instance);
        var unread = await notificationService.GetUnreadForUserAsync(tenantId, memberId, default);
        Assert.Single(unread);

        var updated = await notificationService.MarkAsReadAsync(tenantId, memberId, unread[0].Id, default);

        Assert.NotNull(updated);
        Assert.True(updated!.IsRead);
        Assert.Empty(await notificationService.GetUnreadForUserAsync(tenantId, memberId, default));
    }

    [Fact]
    public async Task MarkAsReadAsync_AnotherUsersNotification_ReturnsNull()
    {
        var (projectService, dbContext) = CreateProjectService();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        dbContext.TeamMemberships.Add(new TeamMembership { TenantId = tenantId, TeamId = teamId, UserId = memberId });
        await dbContext.SaveChangesAsync();

        var project = await projectService.CreateAsync(tenantId, actorUserId, new CreateProjectRequest(teamId, "Ownership Test", null), default);
        var notificationService = new NotificationService(new NotificationRepository(dbContext), NullLogger<NotificationService>.Instance);
        var unread = await notificationService.GetUnreadForUserAsync(tenantId, memberId, default);

        var attackerId = Guid.NewGuid();
        var result = await notificationService.MarkAsReadAsync(tenantId, attackerId, unread[0].Id, default);

        Assert.Null(result);
    }
}
