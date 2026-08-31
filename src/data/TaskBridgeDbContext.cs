using Microsoft.EntityFrameworkCore;
using TaskBridge_API.Notifications;
using TaskBridge_API.Projects;

namespace TaskBridge_API.Data;

public class TaskBridgeDbContext : DbContext
{
    public TaskBridgeDbContext(DbContextOptions<TaskBridgeDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(p => new { p.TenantId, p.TeamId });
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Status).HasConversion<string>();
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasIndex(a => new { a.OrganisationId, a.EntityType, a.EntityId, a.CreatedAt });
            entity.Property(a => a.EventType).HasConversion<string>();
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => new { n.TenantId, n.RecipientUserId, n.IsRead });
            entity.Property(n => n.EventType).HasConversion<string>();
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<TeamMembership>(entity =>
        {
            entity.HasIndex(m => new { m.TenantId, m.TeamId });
        });
    }
}
