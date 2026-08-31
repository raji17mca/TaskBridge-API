using Microsoft.AspNetCore.Http;

namespace TaskBridge_API.Common;

/// <inheritdoc cref="ICurrentUserProvider"/>
public class HttpContextUserProvider : ICurrentUserProvider
{
    private const string UserClaimType = "sub";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public Guid UserId
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User.FindFirst(UserClaimType)?.Value;

            if (!Guid.TryParse(claimValue, out var userId))
            {
                throw new UserResolutionException($"Authenticated request is missing a valid '{UserClaimType}' claim.");
            }

            return userId;
        }
    }
}
