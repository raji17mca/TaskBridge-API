namespace TaskBridge_API.Common;

/// <summary>Resolves the user id of the currently authenticated caller.</summary>
public interface ICurrentUserProvider
{
    /// <summary>The user id from the caller's validated identity. Throws if it cannot be resolved.</summary>
    Guid UserId { get; }
}