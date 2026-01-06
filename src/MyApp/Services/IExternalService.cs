namespace MyApp.Services;

/// <summary>
/// Service interface for external system integration (Anonymous access endpoint)
/// </summary>
public interface IExternalService
{
    /// <summary>
    /// Processes a request from an external system
    /// </summary>
    /// <param name="request">The external request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processing result</returns>
    Task<ExternalResponse> ProcessExternalRequestAsync(ExternalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an external API key
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the API key is valid</returns>
    Task<bool> ValidateApiKeyAsync(string? apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of the external service integration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status information</returns>
    Task<ExternalHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Request from external system
/// </summary>
public sealed class ExternalRequest
{
    /// <summary>
    /// External system identifier
    /// </summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction identifier for tracking
    /// </summary>
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Request type/action
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request payload
    /// </summary>
    public ExternalPayload Payload { get; set; } = new();

    /// <summary>
    /// Callback URL for async responses
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Request priority
    /// </summary>
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;
}

/// <summary>
/// External request payload
/// </summary>
public sealed class ExternalPayload
{
    /// <summary>
    /// Entity type being processed
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity identifier
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Action to perform
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Additional parameters
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = [];
}

/// <summary>
/// Request priority levels
/// </summary>
public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Response to external system
/// </summary>
public sealed class ExternalResponse
{
    /// <summary>
    /// Whether the request was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status code for programmatic handling
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Echo back the transaction ID
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Response data
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = [];

    /// <summary>
    /// Processing metadata
    /// </summary>
    public ResponseMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Response metadata
/// </summary>
public sealed class ResponseMetadata
{
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Server timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request was queued for async processing
    /// </summary>
    public bool IsQueued { get; set; }

    /// <summary>
    /// Estimated completion time for queued requests
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }

    /// <summary>
    /// API version used
    /// </summary>
    public string ApiVersion { get; set; } = "1.0";
}

/// <summary>
/// Health status of external service integration
/// </summary>
public sealed class ExternalHealthStatus
{
    /// <summary>
    /// Whether the external service is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Last successful connection time
    /// </summary>
    public DateTime? LastSuccessfulConnection { get; set; }

    /// <summary>
    /// Last error message if any
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
