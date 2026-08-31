using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge_API.Common;

namespace TaskBridge_API.Notifications;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ICurrentTenantProvider _tenantProvider;

    public AuditController(IAuditService auditService, ICurrentTenantProvider tenantProvider)
    {
        _auditService = auditService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>Internal endpoint used by other services (e.g. the Project Service) to record an audit event.</summary>
    [Authorize(Policy = "InternalService")]
    [HttpPost]
    public async Task<ActionResult<AuditEntryResponse>> RecordEvent(RecordAuditEventRequest request, CancellationToken cancellationToken)
    {
        var entry = await _auditService.RecordEventAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetHistory), new { projectId = entry.EntityId }, entry);
    }

    /// <summary>Gets audit history for a project, optionally filtered by date range and event type.</summary>
    [Authorize]
    [HttpGet("{projectId}")]
    public async Task<ActionResult<IEnumerable<AuditEntryResponse>>> GetHistory(int projectId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] AuditEventType? eventType, CancellationToken cancellationToken)
    {
        var entries = await _auditService.GetHistoryAsync(_tenantProvider.TenantId, projectId, from, to, eventType, cancellationToken);
        return Ok(entries);
    }
}
