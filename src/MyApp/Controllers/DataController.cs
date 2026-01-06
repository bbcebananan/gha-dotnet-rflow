using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Services;

namespace MyApp.Controllers;

/// <summary>
/// SOAP-style data endpoint controller for GetData operations
/// Accessible with Windows Authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DataController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly ILogger<DataController> _logger;

    public DataController(IDataService dataService, ILogger<DataController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Gets data based on query parameters (GET variant of SOAP-style GetData)
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="search">Optional search term</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="maxResults">Maximum results to return (default 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data response with matching items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DataResponse>> GetData(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetData request from user: {User}",
            User.Identity?.Name ?? "Unknown");

        var request = new DataRequest
        {
            Category = category,
            SearchTerm = search,
            FromDate = fromDate,
            ToDate = toDate,
            MaxResults = maxResults
        };

        var result = await _dataService.GetDataAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets data based on POST body (SOAP-style GetData)
    /// </summary>
    /// <param name="request">Data request containing search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data response with matching items</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DataResponse>> PostData(
        [FromBody] DataRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PostData request from user: {User}, category: {Category}",
            User.Identity?.Name ?? "Unknown", request.Category);

        var result = await _dataService.GetDataAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single data item by ID
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The data item if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DataItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataItem>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetById request for ID: {Id} from user: {User}",
            id, User.Identity?.Name ?? "Unknown");

        var item = await _dataService.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { message = $"Data item with ID '{id}' not found" });
        }

        return Ok(item);
    }

    /// <summary>
    /// Gets paginated list of all data items
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result</returns>
    [HttpGet("all")]
    [ProducesResponseType(typeof(PagedResult<DataItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<DataItem>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetAll request page {Page}, size {Size} from user: {User}",
            page, pageSize, User.Identity?.Name ?? "Unknown");

        var result = await _dataService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}

/// <summary>
/// Legacy endpoint mapping for backward compatibility
/// Maps /legacy/api/data to the same data controller
/// </summary>
[ApiController]
[Route("legacy/api/data")]
[Authorize]
public sealed class LegacyDataController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly ILogger<LegacyDataController> _logger;

    public LegacyDataController(IDataService dataService, ILogger<LegacyDataController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Legacy GET endpoint
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DataResponse>> GetLegacyData(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Legacy endpoint accessed by user: {User}. Consider migrating to /api/data",
            User.Identity?.Name ?? "Unknown");

        var request = new DataRequest
        {
            Category = category,
            SearchTerm = search,
            MaxResults = 100
        };

        var result = await _dataService.GetDataAsync(request, cancellationToken);
        return Ok(result);
    }
}
