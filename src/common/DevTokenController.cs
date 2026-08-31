using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace TaskBridge_API.Common;

/// <summary>Dev-only helper for minting test JWTs for local Swagger testing. Refuses to run outside Development.</summary>
[ApiController]
[Route("api/dev/token")]
[AllowAnonymous]
public class DevTokenController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public DevTokenController(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public record IssueTokenRequest(Guid? TenantId, Guid? UserId);
    public record IssueTokenResponse(string Token, Guid TenantId, Guid UserId, DateTime ExpiresAtUtc);

    /// <summary>Issues a short-lived JWT signed with the local dev signing key. Tenant/user ids are random if not supplied.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IssueTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IssueTokenResponse> IssueToken(IssueTokenRequest? request)
    {
        if (!_environment.IsDevelopment())
        {
            // Never available outside local development - there is no real identity provider to delegate to here.
            return NotFound();
        }

        var tenantId = request?.TenantId ?? Guid.NewGuid();
        var userId = request?.UserId ?? Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey configuration is required.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString())
            },
            expires: expiresAt,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new IssueTokenResponse(jwt, tenantId, userId, expiresAt));
    }
}
