using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Services;
using Xunit;

namespace MyApp.Tests.Unit;

public class CertEnrollServiceTests
{
    private readonly Mock<ILogger<CertEnrollService>> _loggerMock;

    public CertEnrollServiceTests()
    {
        _loggerMock = new Mock<ILogger<CertEnrollService>>();
    }

    [Fact]
    public void CertEnrollRequestOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new CertEnrollRequestOptions();

        // Assert
        options.SubjectName.Should().Be(string.Empty);
        options.TemplateName.Should().BeNull();
        options.KeyLength.Should().Be(2048);
        options.KeyUsage.Should().Be("DigitalSignature");
        options.Exportable.Should().BeFalse();
        options.FriendlyName.Should().BeNull();
        options.SubjectAlternativeNames.Should().BeNull();
    }

    [Fact]
    public void CertEnrollRequestOptions_CanSetAllProperties()
    {
        // Arrange
        var options = new CertEnrollRequestOptions
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            KeyLength = 4096,
            KeyUsage = "KeyEncipherment",
            Exportable = true,
            FriendlyName = "Test Certificate",
            SubjectAlternativeNames = ["test.example.com", "www.example.com", "192.168.1.1"]
        };

        // Assert
        options.SubjectName.Should().Be("CN=test.example.com");
        options.TemplateName.Should().Be("WebServer");
        options.KeyLength.Should().Be(4096);
        options.KeyUsage.Should().Be("KeyEncipherment");
        options.Exportable.Should().BeTrue();
        options.FriendlyName.Should().Be("Test Certificate");
        options.SubjectAlternativeNames.Should().HaveCount(3);
    }

    [Fact]
    public void ICertEnrollService_Interface_HasExpectedMethods()
    {
        // Verify the interface has the expected methods
        var interfaceType = typeof(ICertEnrollService);

        interfaceType.GetMethod("CreateCertificateRequest").Should().NotBeNull();
        interfaceType.GetMethod("InstallCertificate").Should().NotBeNull();
        interfaceType.GetMethod("GetAvailableTemplates").Should().NotBeNull();
        interfaceType.GetMethod("CreateSelfSignedCertificate").Should().NotBeNull();
    }

    [Fact]
    public void CertEnrollService_CanBeConstructed()
    {
        // Arrange & Act
        var service = new CertEnrollService(_loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ICertEnrollService>();
    }

    [Fact]
    public void MockedCertEnrollService_CreateCertificateRequest_ReturnsExpectedCsr()
    {
        // Arrange
        var mockService = new Mock<ICertEnrollService>();
        var expectedCsr = "-----BEGIN CERTIFICATE REQUEST-----\nMIIC...base64...AAAA\n-----END CERTIFICATE REQUEST-----";
        var options = new CertEnrollRequestOptions
        {
            SubjectName = "CN=test.example.com",
            KeyLength = 2048
        };

        mockService.Setup(x => x.CreateCertificateRequest(It.IsAny<CertEnrollRequestOptions>()))
          .Returns(expectedCsr);

        // Act
        var result = mockService.Object.CreateCertificateRequest(options);

        // Assert
        result.Should().Be(expectedCsr);
        mockService.Verify(x => x.CreateCertificateRequest(It.IsAny<CertEnrollRequestOptions>()), Times.Once);
    }

    [Fact]
    public void MockedCertEnrollService_InstallCertificate_ReturnsTrue()
    {
        // Arrange
        var mockService = new Mock<ICertEnrollService>();
        mockService.Setup(x => x.InstallCertificate(It.IsAny<string>(), It.IsAny<string>()))
          .Returns(true);

        // Act
        var result = mockService.Object.InstallCertificate("base64-cert-response", "password123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MockedCertEnrollService_GetAvailableTemplates_ReturnsTemplates()
    {
        // Arrange
        var mockService = new Mock<ICertEnrollService>();
        var expectedTemplates = new[] { "WebServer", "CodeSigning", "User", "Computer" };

        mockService.Setup(x => x.GetAvailableTemplates())
          .Returns(expectedTemplates);

        // Act
        var result = mockService.Object.GetAvailableTemplates();

        // Assert
        result.Should().BeEquivalentTo(expectedTemplates);
    }

    [Fact]
    public void MockedCertEnrollService_CreateSelfSignedCertificate_ReturnsThumbprint()
    {
        // Arrange
        var mockService = new Mock<ICertEnrollService>();
        var expectedThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";

        mockService.Setup(x => x.CreateSelfSignedCertificate(It.IsAny<string>(), It.IsAny<int>()))
          .Returns(expectedThumbprint);

        // Act
        var result = mockService.Object.CreateSelfSignedCertificate("CN=Test", 365);

        // Assert
        result.Should().Be(expectedThumbprint);
        result.Should().HaveLength(40); // SHA1 thumbprint length
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void CertEnrollRequestOptions_KeyLength_AcceptsValidValues(int keyLength)
    {
        // Arrange & Act
        var options = new CertEnrollRequestOptions { KeyLength = keyLength };

        // Assert
        options.KeyLength.Should().Be(keyLength);
    }

    [Theory]
    [InlineData("DigitalSignature")]
    [InlineData("KeyEncipherment")]
    [InlineData("DataEncipherment")]
    public void CertEnrollRequestOptions_KeyUsage_AcceptsValidValues(string keyUsage)
    {
        // Arrange & Act
        var options = new CertEnrollRequestOptions { KeyUsage = keyUsage };

        // Assert
        options.KeyUsage.Should().Be(keyUsage);
    }
}
