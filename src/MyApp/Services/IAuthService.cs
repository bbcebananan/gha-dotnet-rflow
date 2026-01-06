using System.Security.Principal;

namespace MyApp.Services;

/// <summary>
/// Service interface for authenticated request processing (Windows Auth endpoint)
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Processes an authenticated request
    /// </summary>
    /// <param name="request">The request to process</param>
    /// <param name="identity">The Windows identity of the authenticated user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processing result</returns>
    Task<AuthResponse> ProcessRequestAsync(AuthRequest request, IIdentity? identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a user has access to a specific resource
    /// </summary>
    /// <param name="identity">The Windows identity to validate</param>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="action">The action to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if access is granted, false otherwise</returns>
    Task<bool> ValidateAccessAsync(IIdentity? identity, string resourceId, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's profile information
    /// </summary>
    /// <param name="identity">The Windows identity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile information</returns>
    Task<UserProfile?> GetUserProfileAsync(IIdentity? identity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for authenticated operations
/// </summary>
public sealed class AuthRequest
{
    /// <summary>
    /// Unique request identifier for tracking
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The operation to perform
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Target resource identifier
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Request payload data
    /// </summary>
    public Dictionary<string, object?> Payload { get; set; } = [];

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for authenticated operations
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result code for programmatic handling
    /// </summary>
    public string ResultCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The authenticated user's name
    /// </summary>
    public string? AuthenticatedUser { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = [];

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User profile information derived from Windows identity
/// </summary>
public sealed class UserProfile
{
    /// <summary>
    /// Windows account name (DOMAIN\username)
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Authentication type (e.g., Negotiate, Kerberos)
    /// </summary>
    public string AuthenticationType { get; set; } = string.Empty;

    /// <summary>
    /// List of groups the user belongs to
    /// </summary>
    public IReadOnlyList<string> Groups { get; set; } = [];

    /// <summary>
    /// User's assigned roles
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>
    /// Profile retrieval timestamp
    /// </summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
