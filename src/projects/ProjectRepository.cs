using Microsoft.EntityFrameworkCore;
using TaskBridge_API.Data;

namespace TaskBridge_API.Projects;

/// <inheritdoc cref="IProjectRepository"/>
public class ProjectRepository : IProjectRepository
{
    private readonly TaskBridgeDbContext _dbContext;

    public ProjectRepository(TaskBridgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Project?> GetByIdAsync(Guid tenantId, int projectId, CancellationToken cancellationToken = default)
    {
        // Tenant filter is part of the query itself, not a check applied after loading the row.
        return _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Project>> GetByTeamAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Where(p => p.TenantId == tenantId && p.TeamId == teamId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Remove(Project project)
    {
        _dbContext.Projects.Remove(project);
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
