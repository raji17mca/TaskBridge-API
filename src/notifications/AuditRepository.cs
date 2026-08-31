using Microsoft.EntityFrameworkCore;
using TaskBridge_API.Data;

namespace TaskBridge_API.Notifications;

/// <inheritdoc cref="IAuditRepository"/>
public class AuditRepository : IAuditRepository
{
    private readonly TaskBridgeDbContext _dbContext;

    public AuditRepository(TaskBridgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditEntries.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditEntry>> GetByProjectAsync(Guid tenantId, string entityId, DateTime? from, DateTime? to, AuditEventType? eventType, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditEntries
            .Where(a => a.OrganisationId == tenantId && a.EntityType == "Project" && a.EntityId == entityId);

        if (from.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= to.Value);
        }

        if (eventType.HasValue)
        {
            query = query.Where(a => a.EventType == eventType.Value);
        }

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
