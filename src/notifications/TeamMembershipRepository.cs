using Microsoft.EntityFrameworkCore;
using TaskBridge_API.Data;

namespace TaskBridge_API.Notifications;

/// <inheritdoc cref="ITeamMembershipRepository"/>
public class TeamMembershipRepository : ITeamMembershipRepository
{
    private readonly TaskBridgeDbContext _dbContext;

    public TeamMembershipRepository(TaskBridgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> GetTeamMemberUserIdsAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamMemberships
            .Where(m => m.TenantId == tenantId && m.TeamId == teamId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);
    }
}
