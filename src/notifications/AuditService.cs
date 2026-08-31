namespace TaskBridge_API.Notifications;

/// <inheritdoc cref="IAuditService"/>
public class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditRepository repository, ILogger<AuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuditEntryResponse> RecordEventAsync(RecordAuditEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OrganisationId == Guid.Empty)
        {
            throw new ArgumentException("Organisation id is required.", nameof(request));
        }

        if (request.ActorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.EntityType) || string.IsNullOrWhiteSpace(request.EntityId))
        {
            throw new ArgumentException("Entity type and entity id are required.", nameof(request));
        }

        var entry = new AuditEntry
        {
            EventType = request.EventType,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            ActorUserId = request.ActorUserId,
            OrganisationId = request.OrganisationId,
            PreviousState = request.PreviousState,
            NewState = request.NewState,
            CreatedAt = DateTime.UtcNow
        };

        // No update/delete path exists anywhere in this service or its repository - immutability is structural, not just policy.
        await _repository.AddAsync(entry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit entry {EventType} recorded for {EntityType} {EntityId} in organisation {OrganisationId}", entry.EventType, entry.EntityType, entry.EntityId, entry.OrganisationId);

        return AuditEntryResponse.FromEntity(entry);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditEntryResponse>> GetHistoryAsync(Guid tenantId, int projectId, DateTime? from, DateTime? to, AuditEventType? eventType, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        var entries = await _repository.GetByProjectAsync(tenantId, projectId.ToString(), from, to, eventType, cancellationToken);
        return entries.Select(AuditEntryResponse.FromEntity).ToList();
    }
}
