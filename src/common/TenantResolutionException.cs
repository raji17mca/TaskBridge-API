namespace TaskBridge_API.Common;

/// <summary>Thrown when the authenticated caller's tenant cannot be determined from the request.</summary>
public class TenantResolutionException : Exception
{
    public TenantResolutionException(string message) : base(message)
    {
    }
}
