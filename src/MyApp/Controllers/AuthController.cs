using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Services;

namespace MyApp.Controllers;

/// <summary>
/// SOAP-style authentication endpoint controller for Windows Auth requests
/// Requires Windows Authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's profile information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfile>> GetProfile(CancellationToken cancellationToken = default)
    {
        var profile = await _authService.GetUserProfileAsync(User.Identity, cancellationToken);
        if (profile == null)
        {
            return Unauthorized(new { message = "Could not retrieve user profile" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Processes an authenticated request (SOAP-style ProcessRequest)
    /// </summary>
    /// <param name="request">The request to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> ProcessRequest(
        [FromBody] AuthRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Operation))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ResultCode = "INVALID_REQUEST",
                Message = "Operation is required"
            });
        }

        _logger.LogInformation(
            "ProcessRequest: {Operation} by user: {User}, request ID: {RequestId}",
            request.Operation, User.Identity?.Name, request.RequestId);

        var result = await _authService.ProcessRequestAsync(request, User.Identity, cancellationToken);

        if (result.ResultCode == "ACCESS_DENIED")
        {
            return Forbid();
        }

        if (result.ResultCode == "AUTH_REQUIRED")
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Checks if the current user has access to a specific resource/action
    /// </summary>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="action">Action to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access check result</returns>
    [HttpGet("access")]
    [ProducesResponseType(typeof(AccessCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessCheckResult>> CheckAccess(
        [FromQuery] string resourceId,
        [FromQuery] string action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceId) || string.IsNullOrWhiteSpace(action))
        {
            return BadRequest(new AccessCheckResult
            {
                HasAccess = false,
                Message = "ResourceId and action are required"
            });
        }

        var hasAccess = await _authService.ValidateAccessAsync(User.Identity, resourceId, action, cancellationToken);
        var profile = await _authService.GetUserProfileAsync(User.Identity, cancellationToken);

        return Ok(new AccessCheckResult
        {
            HasAccess = hasAccess,
            User = User.Identity?.Name ?? "Unknown",
            ResourceId = resourceId,
            Action = action,
            Roles = profile?.Roles ?? [],
            Message = hasAccess ? "Access granted" : "Access denied"
        });
    }
}

/// <summary>
/// Result of an access check
/// </summary>
public sealed class AccessCheckResult
{
    /// <summary>
    /// Whether access is granted
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// User who requested access
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Resource being accessed
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Action requested
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// User's current roles
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
