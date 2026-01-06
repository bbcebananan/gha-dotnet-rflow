using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyApp.Options;
using MyApp.Services;

namespace MyApp.Tests.Unit;

/// <summary>
/// Unit tests for the DataService
/// </summary>
public class DataServiceTests
{
    private readonly Mock<ILogger<DataService>> _loggerMock;
    private readonly Mock<IOptions<AppConfig>> _configMock;
    private readonly DataService _sut;

    public DataServiceTests()
    {
        _loggerMock = new Mock<ILogger<DataService>>();
        _configMock = new Mock<IOptions<AppConfig>>();
        _configMock.Setup(x => x.Value).Returns(new AppConfig
        {
            MaxConcurrentRequests = 100,
            EnableDetailedErrors = true
        });

        _sut = new DataService(_loggerMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task GetDataAsync_WithNoFilters_ReturnsAllActiveItems()
    {
        // Arrange
        var request = new DataRequest { MaxResults = 100 };

        // Act
        var result = await _sut.GetDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(x => x.IsActive);
    }

    [Fact]
    public async Task GetDataAsync_WithCategoryFilter_ReturnsFilteredItems()
    {
        // Arrange
        var request = new DataRequest
        {
            Category = "Finance",
            MaxResults = 50
        };

        // Act
        var result = await _sut.GetDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Items.Should().OnlyContain(x =>
            x.Category.Equals("Finance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDataAsync_WithSearchTerm_ReturnsMatchingItems()
    {
        // Arrange
        var request = new DataRequest
        {
            SearchTerm = "Operations",
            MaxResults = 50
        };

        // Act
        var result = await _sut.GetDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Items.Should().AllSatisfy(x =>
            x.Name.Contains("Operations", StringComparison.OrdinalIgnoreCase) ||
            x.Description.Contains("Operations", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDataAsync_WithMaxResults_RespectsLimit()
    {
        // Arrange
        var request = new DataRequest { MaxResults = 5 };

        // Act
        var result = await _sut.GetDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Items.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetDataAsync_ReturnsTimestamp()
    {
        // Arrange
        var request = new DataRequest();
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await _sut.GetDataAsync(request);
        var afterCall = DateTime.UtcNow;

        // Assert
        result.Timestamp.Should().BeOnOrAfter(beforeCall);
        result.Timestamp.Should().BeOnOrBefore(afterCall);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsItem()
    {
        // Arrange - First get some data to find a valid ID
        var allData = await _sut.GetDataAsync(new DataRequest { MaxResults = 1 });
        var existingId = allData.Items.First().Id;

        // Act
        var result = await _sut.GetByIdAsync(existingId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingId);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = "NON-EXISTENT-ID-12345";

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _sut.GetAllAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.Items.Should().HaveCountLessOrEqualTo(pageSize);
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllAsync_SecondPage_ReturnsDifferentItems()
    {
        // Arrange
        var pageSize = 5;

        // Act
        var page1 = await _sut.GetAllAsync(1, pageSize);
        var page2 = await _sut.GetAllAsync(2, pageSize);

        // Assert
        page1.Items.Should().NotBeEquivalentTo(page2.Items);
    }

    [Fact]
    public async Task GetAllAsync_CalculatesTotalPages()
    {
        // Arrange
        var pageSize = 10;

        // Act
        var result = await _sut.GetAllAsync(1, pageSize);

        // Assert
        var expectedTotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);
        result.TotalPages.Should().Be(expectedTotalPages);
    }

    [Fact]
    public async Task GetAllAsync_HasNextPage_WhenNotOnLastPage()
    {
        // Arrange
        var pageSize = 5;

        // Act
        var result = await _sut.GetAllAsync(1, pageSize);

        // Assert
        if (result.TotalCount > pageSize)
        {
            result.HasNextPage.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetAllAsync_HasPreviousPage_WhenNotOnFirstPage()
    {
        // Arrange
        var pageSize = 5;

        // Act
        var result = await _sut.GetAllAsync(2, pageSize);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10)]  // Invalid page
    [InlineData(-1, 10)] // Negative page
    public async Task GetAllAsync_WithInvalidPageNumber_DefaultsToPageOne(int pageNumber, int pageSize)
    {
        // Act
        var result = await _sut.GetAllAsync(pageNumber, pageSize);

        // Assert
        result.PageNumber.Should().Be(1);
    }

    [Theory]
    [InlineData(1, 0)]   // Zero page size
    [InlineData(1, -1)]  // Negative page size
    public async Task GetAllAsync_WithInvalidPageSize_DefaultsToTen(int pageNumber, int pageSize)
    {
        // Act
        var result = await _sut.GetAllAsync(pageNumber, pageSize);

        // Assert
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_WithExcessivePageSize_CapsAtHundred()
    {
        // Act
        var result = await _sut.GetAllAsync(1, 500);

        // Assert
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetDataAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-180);
        var toDate = DateTime.UtcNow.AddDays(-30);
        var request = new DataRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            MaxResults = 100
        };

        // Act
        var result = await _sut.GetDataAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Items.Should().AllSatisfy(x =>
        {
            x.CreatedAt.Should().BeOnOrAfter(fromDate);
            x.CreatedAt.Should().BeOnOrBefore(toDate);
        });
    }
}
