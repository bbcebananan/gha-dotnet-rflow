using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MyApp.Options;

namespace MyApp.Services;

/// <summary>
/// Implementation of the external service for anonymous access endpoints
/// </summary>
public sealed class ExternalService : IExternalService
{
    private readonly ILogger<ExternalService> _logger;
    private readonly AppConfig _config;
    private readonly IExternalRestApiClient _externalApiClient;

    // API key validation (in production, use secure storage)
    private static readonly HashSet<string> ValidApiKeys = new(StringComparer.Ordinal)
    {
        "demo-api-key-12345",
        "test-api-key-67890"
    };

    private DateTime? _lastSuccessfulConnection;
    private string? _lastError;

    public ExternalService(
        ILogger<ExternalService> logger,
        IOptions<AppConfig> config,
        IExternalRestApiClient externalApiClient)
    {
        _logger = logger;
        _config = config.Value;
        _externalApiClient = externalApiClient;
    }

    /// <inheritdoc />
    public async Task<ExternalResponse> ProcessExternalRequestAsync(
        ExternalRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Processing external request {TransactionId} from {SystemId}, type: {RequestType}",
                request.TransactionId, request.SystemId, request.RequestType);

            // Validate API key
            if (!await ValidateApiKeyAsync(request.ApiKey, cancellationToken))
            {
                _logger.LogWarning("Invalid API key for request {TransactionId}", request.TransactionId);
                return CreateErrorResponse(request.TransactionId, 401, "INVALID_API_KEY",
                    "The provided API key is invalid or expired", stopwatch.ElapsedMilliseconds);
            }

            // Validate request
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsValid)
            {
                return CreateErrorResponse(request.TransactionId, 400, "VALIDATION_ERROR",
                    validationResult.ErrorMessage ?? "Request validation failed", stopwatch.ElapsedMilliseconds);
            }

            // Process based on request type
            var result = await ProcessRequestByTypeAsync(request, cancellationToken);

            stopwatch.Stop();
            _lastSuccessfulConnection = DateTime.UtcNow;

            return new ExternalResponse
            {
                Success = true,
                StatusCode = 200,
                Message = "Request processed successfully",
                TransactionId = request.TransactionId,
                Data = result,
                Metadata = new ResponseMetadata
                {
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    IsQueued = request.Priority == RequestPriority.Low
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing external request {TransactionId}", request.TransactionId);
            _lastError = ex.Message;
            stopwatch.Stop();

            return CreateErrorResponse(
                request.TransactionId,
                500,
                "INTERNAL_ERROR",
                _config.EnableDetailedErrors ? ex.Message : "An internal error occurred",
                stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateApiKeyAsync(string? apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        await Task.Delay(5, cancellationToken); // Simulate async validation

        // Check against configured API key if set
        if (!string.IsNullOrWhiteSpace(_config.ExternalApiKey))
        {
            // In production, the configured key would be encrypted with DPAPI
            // Here we do a simple comparison
            return SecureCompare(apiKey, _config.ExternalApiKey);
        }

        // Fall back to demo keys
        return ValidApiKeys.Contains(apiKey);
    }

    /// <inheritdoc />
    public async Task<ExternalHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try to ping the external API
            var isReachable = await _externalApiClient.PingAsync(cancellationToken);
            stopwatch.Stop();

            if (isReachable)
            {
                _lastSuccessfulConnection = DateTime.UtcNow;
                _lastError = null;
            }

            return new ExternalHealthStatus
            {
                IsHealthy = isReachable,
                Status = isReachable ? "Connected" : "Unreachable",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                LastSuccessfulConnection = _lastSuccessfulConnection,
                LastError = _lastError
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _lastError = ex.Message;

            return new ExternalHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                LastSuccessfulConnection = _lastSuccessfulConnection,
                LastError = ex.Message
            };
        }
    }

    /// <summary>
    /// Validates the request structure
    /// </summary>
    private static (bool IsValid, string? ErrorMessage) ValidateRequest(ExternalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SystemId))
        {
            return (false, "SystemId is required");
        }

        if (string.IsNullOrWhiteSpace(request.RequestType))
        {
            return (false, "RequestType is required");
        }

        if (request.Payload == null)
        {
            return (false, "Payload is required");
        }

        return (true, null);
    }

    /// <summary>
    /// Processes the request based on its type
    /// </summary>
    private async Task<Dictionary<string, object?>> ProcessRequestByTypeAsync(
        ExternalRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulate processing

        return request.RequestType.ToUpperInvariant() switch
        {
            "SYNC_DATA" => await ProcessSyncDataAsync(request.Payload, cancellationToken),
            "QUERY" => await ProcessQueryAsync(request.Payload, cancellationToken),
            "NOTIFY" => await ProcessNotificationAsync(request.Payload, cancellationToken),
            "WEBHOOK" => await ProcessWebhookAsync(request, cancellationToken),
            _ => new Dictionary<string, object?>
            {
                ["requestType"] = request.RequestType,
                ["processed"] = true,
                ["message"] = "Request type acknowledged"
            }
        };
    }

    private async Task<Dictionary<string, object?>> ProcessSyncDataAsync(
        ExternalPayload payload,
        CancellationToken cancellationToken)
    {
        await Task.Delay(25, cancellationToken);
        return new Dictionary<string, object?>
        {
            ["action"] = "sync",
            ["entityType"] = payload.EntityType,
            ["entityId"] = payload.EntityId,
            ["synced"] = true,
            ["syncedAt"] = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object?>> ProcessQueryAsync(
        ExternalPayload payload,
        CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
        return new Dictionary<string, object?>
        {
            ["action"] = "query",
            ["entityType"] = payload.EntityType,
            ["found"] = true,
            ["resultCount"] = 5,
            ["results"] = new[] { "result1", "result2", "result3" }
        };
    }

    private async Task<Dictionary<string, object?>> ProcessNotificationAsync(
        ExternalPayload payload,
        CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return new Dictionary<string, object?>
        {
            ["action"] = "notify",
            ["acknowledged"] = true,
            ["notificationType"] = payload.Action,
            ["processedAt"] = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object?>> ProcessWebhookAsync(
        ExternalRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(15, cancellationToken);

        var hasCallback = !string.IsNullOrWhiteSpace(request.CallbackUrl);

        return new Dictionary<string, object?>
        {
            ["action"] = "webhook",
            ["hasCallback"] = hasCallback,
            ["callbackScheduled"] = hasCallback,
            ["priority"] = request.Priority.ToString()
        };
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    private static ExternalResponse CreateErrorResponse(
        string transactionId,
        int statusCode,
        string errorCode,
        string message,
        long processingTimeMs)
    {
        return new ExternalResponse
        {
            Success = false,
            StatusCode = statusCode,
            ErrorCode = errorCode,
            Message = message,
            TransactionId = transactionId,
            Metadata = new ResponseMetadata
            {
                ProcessingTimeMs = processingTimeMs
            }
        };
    }

    /// <summary>
    /// Performs a constant-time comparison of two strings to prevent timing attacks
    /// </summary>
    private static bool SecureCompare(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
