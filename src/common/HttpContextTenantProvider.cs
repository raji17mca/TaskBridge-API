using Microsoft.AspNetCore.Http;

namespace TaskBridge_API.Common;

/// <inheritdoc cref="ICurrentTenantProvider"/>
public class HttpContextTenantProvider : ICurrentTenantProvider
{
    private const string TenantClaimType = "tenant_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public Guid TenantId
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimType)?.Value;

            // Tenant must come from a claim in a validated JWT, never a client-supplied header - headers are trivially spoofable.
            if (!Guid.TryParse(claimValue, out var tenantId))
            {
                throw new TenantResolutionException($"Authenticated request is missing a valid '{TenantClaimType}' claim.");
            }

            return tenantId;
        }
    }
}
