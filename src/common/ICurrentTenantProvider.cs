namespace TaskBridge_API.Common;

/// <summary>Resolves the tenant id of the currently authenticated caller.</summary>
public interface ICurrentTenantProvider
{
    /// <summary>The tenant id from the caller's validated identity. Throws if it cannot be resolved.</summary>
    Guid TenantId { get; }
}
