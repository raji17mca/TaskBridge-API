namespace TaskBridge_API.Notifications;

/// <summary>Resolves team membership. Minimal placeholder until a dedicated Team Service/model exists.</summary>
public interface ITeamMembershipRepository
{
    /// <summary>Lists the user ids belonging to a team within a tenant.</summary>
    Task<IReadOnlyList<Guid>> GetTeamMemberUserIdsAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken = default);
}
