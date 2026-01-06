using Microsoft.Extensions.Options;
using MyApp.Options;

namespace MyApp.Services;

/// <summary>
/// Implementation of the data service for SOAP-style GetData endpoint
/// </summary>
public sealed class DataService : IDataService
{
    private readonly ILogger<DataService> _logger;
    private readonly AppConfig _config;

    // In-memory data store for demonstration (replace with actual data source)
    private static readonly List<DataItem> DataStore = GenerateSampleData();

    public DataService(ILogger<DataService> logger, IOptions<AppConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <inheritdoc />
    public async Task<DataResponse> GetDataAsync(DataRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing data request with category: {Category}, search: {Search}",
                request.Category, request.SearchTerm);

            await Task.Delay(10, cancellationToken); // Simulate async operation

            var query = DataStore.AsQueryable();

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                query = query.Where(x => x.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
            }

            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLowerInvariant();
                query = query.Where(x =>
                    x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // Apply date range filter
            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= request.ToDate.Value);
            }

            // Only return active items
            query = query.Where(x => x.IsActive);

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(x => x.CreatedAt)
                .Take(Math.Min(request.MaxResults, _config.MaxConcurrentRequests))
                .ToList();

            _logger.LogInformation("Retrieved {Count} items out of {Total} total", items.Count, totalCount);

            return new DataResponse
            {
                Success = true,
                Items = items,
                TotalCount = totalCount,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing data request");
            return new DataResponse
            {
                Success = false,
                ErrorMessage = _config.EnableDetailedErrors ? ex.Message : "An error occurred processing the request",
                Items = [],
                TotalCount = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<DataItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving data item by ID: {Id}", id);
        await Task.Delay(5, cancellationToken);

        return DataStore.FirstOrDefault(x =>
            x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && x.IsActive);
    }

    /// <inheritdoc />
    public async Task<PagedResult<DataItem>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        _logger.LogDebug("Retrieving all data items, page {PageNumber}, size {PageSize}", pageNumber, pageSize);
        await Task.Delay(5, cancellationToken);

        var query = DataStore.Where(x => x.IsActive).OrderByDescending(x => x.CreatedAt);
        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<DataItem>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Generates sample data for demonstration purposes
    /// </summary>
    private static List<DataItem> GenerateSampleData()
    {
        var categories = new[] { "Finance", "Operations", "Sales", "Support", "Development" };
        var random = new Random(42); // Fixed seed for reproducibility
        var items = new List<DataItem>();

        for (int i = 1; i <= 50; i++)
        {
            var category = categories[random.Next(categories.Length)];
            items.Add(new DataItem
            {
                Id = $"DATA-{i:D5}",
                Name = $"{category} Item {i}",
                Category = category,
                Description = $"This is a sample {category.ToLower()} data item with ID {i}. " +
                              $"It contains important information for the {category} department.",
                Value = Math.Round((decimal)(random.NextDouble() * 10000), 2),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                ModifiedAt = random.Next(2) == 0 ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null,
                IsActive = true,
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = "system",
                    ["priority"] = random.Next(1, 4).ToString(),
                    ["region"] = random.Next(2) == 0 ? "EMEA" : "APAC"
                }
            });
        }

        return items;
    }
}
