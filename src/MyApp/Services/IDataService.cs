namespace MyApp.Services;

/// <summary>
/// Service interface for data retrieval operations (SOAP-style GetData endpoint)
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Retrieves data based on the specified request parameters
    /// </summary>
    /// <param name="request">The data request containing search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the requested data</returns>
    Task<DataResponse> GetDataAsync(DataRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves data by unique identifier
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The data item if found, null otherwise</returns>
    Task<DataItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all available data items with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of data items</returns>
    Task<PagedResult<DataItem>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for data queries
/// </summary>
public sealed class DataRequest
{
    /// <summary>
    /// Optional filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional search term
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Start date for date range filter
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for date range filter
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 100;
}

/// <summary>
/// Response model for data queries
/// </summary>
public sealed class DataResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The retrieved data items
    /// </summary>
    public IReadOnlyList<DataItem> Items { get; set; } = [];

    /// <summary>
    /// Total count of matching items (for pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Timestamp when the data was retrieved
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual data item
/// </summary>
public sealed class DataItem
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category classification
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Associated value or amount
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Whether this item is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Generic paginated result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Items on the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
