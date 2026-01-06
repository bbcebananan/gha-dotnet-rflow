using System.Security.Principal;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyApp.Options;
using MyApp.Services;
using Xunit;

namespace MyApp.Tests.Unit;

/// <summary>
/// Unit tests for the AuthService
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IOptions<AppConfig>> _configMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _loggerMock = new Mock<ILogger<AuthService>>();
        _configMock = new Mock<IOptions<AppConfig>>();
        _configMock.Setup(x => x.Value).Returns(new AppConfig
        {
            EnableDetailedErrors = true
        });

        _sut = new AuthService(_loggerMock.Object, _configMock.Object);
    }

    private static Mock<IIdentity> CreateMockIdentity(string name, bool isAuthenticated = true)
    {
        var identityMock = new Mock<IIdentity>();
        identityMock.Setup(x => x.Name).Returns(name);
        identityMock.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        identityMock.Setup(x => x.AuthenticationType).Returns("Negotiate");
        return identityMock;
    }

    [Fact]
    public async Task ProcessRequestAsync_WithNullIdentity_ReturnsAuthRequired()
    {
        // Arrange
        var request = new AuthRequest { Operation = "GET_STATUS" };

        // Act
        var result = await _sut.ProcessRequestAsync(request, null);

        // Assert
        result.Success.Should().BeFalse();
        result.ResultCode.Should().Be("AUTH_REQUIRED");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithUnauthenticatedIdentity_ReturnsAuthRequired()
    {
        // Arrange
        var request = new AuthRequest { Operation = "GET_STATUS" };
        var identity = CreateMockIdentity("DOMAIN\\user", isAuthenticated: false);

        // Act
        var result = await _sut.ProcessRequestAsync(request, identity.Object);

        // Assert
        result.Success.Should().BeFalse();
        result.ResultCode.Should().Be("AUTH_REQUIRED");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithValidIdentity_ReturnsSuccess()
    {
        // Arrange
        var request = new AuthRequest { Operation = "GET_STATUS" };
        var identity = CreateMockIdentity("DOMAIN\\regularuser");

        // Act
        var result = await _sut.ProcessRequestAsync(request, identity.Object);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultCode.Should().Be("SUCCESS");
        result.AuthenticatedUser.Should().Be("DOMAIN\\regularuser");
    }

    [Fact]
    public async Task ProcessRequestAsync_IncludesProcessingTime()
    {
        // Arrange
        var request = new AuthRequest { Operation = "GET_STATUS" };
        var identity = CreateMockIdentity("DOMAIN\\user");

        // Act
        var result = await _sut.ProcessRequestAsync(request, identity.Object);

        // Assert
        result.ProcessingTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessRequestAsync_IncludesTimestamp()
    {
        // Arrange
        var request = new AuthRequest { Operation = "GET_STATUS" };
        var identity = CreateMockIdentity("DOMAIN\\user");
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await _sut.ProcessRequestAsync(request, identity.Object);

        // Assert
        result.Timestamp.Should().BeOnOrAfter(beforeCall);
    }

    [Theory]
    [InlineData("GET_STATUS")]
    [InlineData("UPDATE_CONFIG")]
    [InlineData("PROCESS_DATA")]
    [InlineData("CUSTOM_OPERATION")]
    public async Task ProcessRequestAsync_HandlesVariousOperations(string operation)
    {
        // Arrange
        var request = new AuthRequest { Operation = operation };
        var identity = CreateMockIdentity("DOMAIN\\adminuser");

        // Act
        var result = await _sut.ProcessRequestAsync(request, identity.Object);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAccessAsync_WithNullIdentity_ReturnsFalse()
    {
        // Act
        var result = await _sut.ValidateAccessAsync(null, "resource", "READ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAccessAsync_WithUnauthenticatedIdentity_ReturnsFalse()
    {
        // Arrange
        var identity = CreateMockIdentity("DOMAIN\\user", isAuthenticated: false);

        // Act
        var result = await _sut.ValidateAccessAsync(identity.Object, "resource", "READ");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("READ", true)]
    [InlineData("WRITE", false)]
    [InlineData("DELETE", false)]
    [InlineData("ADMIN", false)]
    public async Task ValidateAccessAsync_RegularUser_HasCorrectPermissions(string action, bool expected)
    {
        // Arrange - Regular user (not admin or manager)
        var identity = CreateMockIdentity("DOMAIN\\regularuser");

        // Act
        var result = await _sut.ValidateAccessAsync(identity.Object, "resource", action);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("READ", true)]
    [InlineData("WRITE", true)]
    [InlineData("DELETE", true)]
    [InlineData("ADMIN", true)]
    public async Task ValidateAccessAsync_AdminUser_HasAllPermissions(string action, bool expected)
    {
        // Arrange - Admin user (name contains 'admin')
        var identity = CreateMockIdentity("DOMAIN\\adminuser");

        // Act
        var result = await _sut.ValidateAccessAsync(identity.Object, "resource", action);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNullIdentity_ReturnsNull()
    {
        // Act
        var result = await _sut.GetUserProfileAsync(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserProfileAsync_WithEmptyName_ReturnsNull()
    {
        // Arrange
        var identity = CreateMockIdentity(string.Empty);

        // Act
        var result = await _sut.GetUserProfileAsync(identity.Object);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserProfileAsync_WithValidIdentity_ReturnsProfile()
    {
        // Arrange
        var identity = CreateMockIdentity("DOMAIN\\testuser");

        // Act
        var result = await _sut.GetUserProfileAsync(identity.Object);

        // Assert
        result.Should().NotBeNull();
        result!.AccountName.Should().Be("DOMAIN\\testuser");
        result.DisplayName.Should().Be("testuser");
        result.IsAuthenticated.Should().BeTrue();
        result.AuthenticationType.Should().Be("Negotiate");
    }

    [Fact]
    public async Task GetUserProfileAsync_IncludesRoles()
    {
        // Arrange
        var identity = CreateMockIdentity("DOMAIN\\regularuser");

        // Act
        var result = await _sut.GetUserProfileAsync(identity.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().NotBeEmpty();
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetUserProfileAsync_AdminUser_HasAdminRole()
    {
        // Arrange
        var identity = CreateMockIdentity("DOMAIN\\administrator");

        // Act
        var result = await _sut.GetUserProfileAsync(identity.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetUserProfileAsync_IncludesGroups()
    {
        // Arrange
        var identity = CreateMockIdentity("DOMAIN\\user");

        // Act
        var result = await _sut.GetUserProfileAsync(identity.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Groups.Should().NotBeEmpty();
        result.Groups.Should().Contain("Domain Users");
    }

    [Fact]
    public async Task GetUserProfileAsync_IncludesTimestamp()
    {
        // Arrange
        var identity = CreateMockIdentity("DOMAIN\\user");
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await _sut.GetUserProfileAsync(identity.Object);

        // Assert
        result.Should().NotBeNull();
        result!.RetrievedAt.Should().BeOnOrAfter(beforeCall);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithAccessDenied_ReturnsCorrectCode()
    {
        // Arrange
        var request = new AuthRequest
        {
            Operation = "DELETE",  // Requires Admin role
            ResourceId = "some-resource"
        };
        var identity = CreateMockIdentity("DOMAIN\\regularuser"); // Not an admin

        // Act
        var result = await _sut.ProcessRequestAsync(request, identity.Object);

        // Assert
        result.Success.Should().BeFalse();
        result.ResultCode.Should().Be("ACCESS_DENIED");
    }
}
