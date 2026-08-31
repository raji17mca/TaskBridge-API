namespace TaskBridge_API.Common;

/// <summary>Thrown when the authenticated caller's user id cannot be determined from the request.</summary>
public class UserResolutionException : Exception
{
    public UserResolutionException(string message) : base(message)
    {
    }
}
