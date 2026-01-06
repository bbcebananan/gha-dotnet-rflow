using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Services;

namespace MyApp.Controllers;

/// <summary>
/// SOAP-style external endpoint controller for anonymous access from external systems
/// Uses API key authentication instead of Windows Auth
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class ExternalController : ControllerBase
{
    private readonly IExternalService _externalService;
    private readonly ILogger<ExternalController> _logger;

    public ExternalController(IExternalService externalService, ILogger<ExternalController> logger)
    {
        _externalService = externalService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the health status of the external integration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ExternalHealthStatus), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExternalHealthStatus>> GetHealth(CancellationToken cancellationToken = default)
    {
        var status = await _externalService.GetHealthStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Processes an external request (SOAP-style ProcessRequest for external systems)
    /// </summary>
    /// <param name="request">The external request</param>
    /// <param name="apiKey">API key from header (alternative to request body)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExternalResponse>> ProcessRequest(
        [FromBody] ExternalRequest request,
        [FromHeader(Name = "X-Api-Key")] string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        // Allow API key from header or request body
        if (!string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(request.ApiKey))
        {
            request.ApiKey = apiKey;
        }

        _logger.LogInformation(
            "External request {TransactionId} from {SystemId}, type: {RequestType}",
            request.TransactionId, request.SystemId, request.RequestType);

        var result = await _externalService.ProcessExternalRequestAsync(request, cancellationToken);

        return result.StatusCode switch
        {
            200 => Ok(result),
            400 => BadRequest(result),
            401 => Unauthorized(result),
            _ => StatusCode(result.StatusCode, result)
        };
    }

    /// <summary>
    /// Validates an API key
    /// </summary>
    /// <param name="apiKey">API key to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ApiKeyValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiKeyValidationResult>> ValidateApiKey(
        [FromHeader(Name = "X-Api-Key")] string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var isValid = await _externalService.ValidateApiKeyAsync(apiKey, cancellationToken);

        return Ok(new ApiKeyValidationResult
        {
            IsValid = isValid,
            Message = isValid ? "API key is valid" : "API key is invalid or expired"
        });
    }

    /// <summary>
    /// Webhook endpoint for receiving callbacks from external systems
    /// </summary>
    /// <param name="request">Webhook payload</param>
    /// <param name="signature">Request signature for validation</param>
    /// <param name="apiKey">API key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Acknowledgment</returns>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExternalResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExternalResponse>> Webhook(
        [FromBody] ExternalRequest request,
        [FromHeader(Name = "X-Signature")] string? signature = null,
        [FromHeader(Name = "X-Api-Key")] string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        // Override request type for webhook processing
        request.RequestType = "WEBHOOK";

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.ApiKey = apiKey;
        }

        _logger.LogInformation(
            "Webhook received {TransactionId} from {SystemId}, signature present: {HasSignature}",
            request.TransactionId, request.SystemId, !string.IsNullOrEmpty(signature));

        var result = await _externalService.ProcessExternalRequestAsync(request, cancellationToken);

        return result.StatusCode switch
        {
            200 => Ok(result),
            400 => BadRequest(result),
            401 => Unauthorized(result),
            _ => StatusCode(result.StatusCode, result)
        };
    }
}

/// <summary>
/// Result of API key validation
/// </summary>
public sealed class ApiKeyValidationResult
{
    /// <summary>
    /// Whether the API key is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
