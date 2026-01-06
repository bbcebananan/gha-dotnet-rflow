using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Options;
using MyApp.Options;

namespace MyApp.Services;

/// <summary>
/// Implementation of the authentication service for Windows Auth endpoints
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly AppConfig _config;

    // Simulated role mappings (in production, this would come from Active Directory or a database)
    private static readonly Dictionary<string, string[]> RoleMappings = new()
    {
        ["Administrators"] = ["Admin", "Manager", "User"],
        ["Domain Users"] = ["User"],
        ["Power Users"] = ["Manager", "User"]
    };

    public AuthService(ILogger<AuthService> logger, IOptions<AppConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> ProcessRequestAsync(
        AuthRequest request,
        IIdentity? identity,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (identity == null || !identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthenticated request received for operation: {Operation}", request.Operation);
                return new AuthResponse
                {
                    Success = false,
                    ResultCode = "AUTH_REQUIRED",
                    Message = "Authentication is required for this operation",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var userName = identity.Name ?? "Unknown";
            _logger.LogInformation(
                "Processing authenticated request {RequestId} from {User}, operation: {Operation}",
                request.RequestId, userName, request.Operation);

            await Task.Delay(50, cancellationToken); // Simulate processing time

            // Validate access based on operation
            var hasAccess = await ValidateAccessAsync(
                identity, request.ResourceId ?? "", request.Operation, cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning("Access denied for user {User} on operation {Operation}",
                    userName, request.Operation);
                return new AuthResponse
                {
                    Success = false,
                    ResultCode = "ACCESS_DENIED",
                    Message = "You do not have permission to perform this operation",
                    AuthenticatedUser = userName,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Process the operation
            var result = await ExecuteOperationAsync(request, userName, cancellationToken);

            stopwatch.Stop();
            return new AuthResponse
            {
                Success = true,
                ResultCode = "SUCCESS",
                Message = $"Operation '{request.Operation}' completed successfully",
                AuthenticatedUser = userName,
                Data = result,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {RequestId}", request.RequestId);
            stopwatch.Stop();
            return new AuthResponse
            {
                Success = false,
                ResultCode = "INTERNAL_ERROR",
                Message = _config.EnableDetailedErrors ? ex.Message : "An internal error occurred",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAccessAsync(
        IIdentity? identity,
        string resourceId,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (identity == null || !identity.IsAuthenticated)
        {
            return false;
        }

        await Task.Delay(5, cancellationToken); // Simulate async validation

        // Get user profile to check roles
        var profile = await GetUserProfileAsync(identity, cancellationToken);
        if (profile == null)
        {
            return false;
        }

        // Define action-to-role mappings
        var allowedRoles = action.ToUpperInvariant() switch
        {
            "READ" => new[] { "User", "Manager", "Admin" },
            "WRITE" or "UPDATE" => new[] { "Manager", "Admin" },
            "DELETE" or "ADMIN" => new[] { "Admin" },
            _ => new[] { "User", "Manager", "Admin" } // Default: allow all authenticated users
        };

        return profile.Roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<UserProfile?> GetUserProfileAsync(
        IIdentity? identity,
        CancellationToken cancellationToken = default)
    {
        if (identity == null || string.IsNullOrEmpty(identity.Name))
        {
            return null;
        }

        await Task.Delay(10, cancellationToken); // Simulate async lookup

        var accountName = identity.Name;
        var displayName = ExtractDisplayName(accountName);

        // Determine roles based on simulated group membership
        var groups = GetSimulatedGroups(accountName);
        var roles = DetermineRoles(groups);

        return new UserProfile
        {
            AccountName = accountName,
            DisplayName = displayName,
            IsAuthenticated = identity.IsAuthenticated,
            AuthenticationType = identity.AuthenticationType ?? "Unknown",
            Groups = groups,
            Roles = roles
        };
    }

    /// <summary>
    /// Executes the requested operation
    /// </summary>
    private async Task<Dictionary<string, object?>> ExecuteOperationAsync(
        AuthRequest request,
        string userName,
        CancellationToken cancellationToken)
    {
        await Task.Delay(25, cancellationToken); // Simulate operation execution

        return request.Operation.ToUpperInvariant() switch
        {
            "GET_STATUS" => new Dictionary<string, object?>
            {
                ["status"] = "Active",
                ["lastUpdated"] = DateTime.UtcNow,
                ["requestedBy"] = userName
            },
            "UPDATE_CONFIG" => new Dictionary<string, object?>
            {
                ["updated"] = true,
                ["configVersion"] = 2,
                ["updatedBy"] = userName,
                ["timestamp"] = DateTime.UtcNow
            },
            "PROCESS_DATA" => new Dictionary<string, object?>
            {
                ["processed"] = true,
                ["itemCount"] = request.Payload.Count,
                ["processedBy"] = userName,
                ["duration"] = "25ms"
            },
            _ => new Dictionary<string, object?>
            {
                ["operation"] = request.Operation,
                ["executed"] = true,
                ["executedBy"] = userName,
                ["timestamp"] = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Extracts display name from Windows account name
    /// </summary>
    private static string ExtractDisplayName(string accountName)
    {
        var parts = accountName.Split('\\');
        return parts.Length > 1 ? parts[1] : accountName;
    }

    /// <summary>
    /// Gets simulated Windows groups for demonstration
    /// </summary>
    private static List<string> GetSimulatedGroups(string accountName)
    {
        // In production, this would query Active Directory
        var groups = new List<string> { "Domain Users" };

        // Simulate admin detection
        if (accountName.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            groups.Add("Administrators");
        }
        else if (accountName.Contains("manager", StringComparison.OrdinalIgnoreCase))
        {
            groups.Add("Power Users");
        }

        return groups;
    }

    /// <summary>
    /// Determines application roles based on Windows groups
    /// </summary>
    private static List<string> DetermineRoles(IEnumerable<string> groups)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (RoleMappings.TryGetValue(group, out var mappedRoles))
            {
                foreach (var role in mappedRoles)
                {
                    roles.Add(role);
                }
            }
        }

        // Ensure at least User role for authenticated users
        if (roles.Count == 0)
        {
            roles.Add("User");
        }

        return roles.ToList();
    }
}
