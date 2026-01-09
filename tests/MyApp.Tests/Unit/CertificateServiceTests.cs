using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Services;
using Xunit;

namespace MyApp.Tests.Unit;

public class CertificateServiceTests
{
    private readonly Mock<ILogger<CertificateService>> _loggerMock;
    private readonly CertificateService _service;

    public CertificateServiceTests()
    {
        _loggerMock = new Mock<ILogger<CertificateService>>();
        _service = new CertificateService(_loggerMock.Object);
    }

    [Fact]
    public void GenerateCertificateRequest_ValidInput_ReturnsCsr()
    {
        // Arrange
        var subjectName = "CN=test.example.com";
        var keyLength = 2048;

        // Act
        var csr = _service.GenerateCertificateRequest(subjectName, keyLength);

        // Assert
        csr.Should().NotBeNullOrEmpty();
        // CSR should be base64 encoded
        var decoded = Convert.FromBase64String(csr);
        decoded.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void GenerateCertificateRequest_DifferentKeyLengths_Succeeds(int keyLength)
    {
        // Arrange
        var subjectName = "CN=test.example.com";

        // Act
        var csr = _service.GenerateCertificateRequest(subjectName, keyLength);

        // Assert
        csr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void IssueCertificate_ValidInput_ReturnsCertificate()
    {
        // Arrange
        var csr = _service.GenerateCertificateRequest("CN=test", 2048);
        var templateName = "WebServer";
        var validity = TimeSpan.FromDays(365);

        // Act
        var certificate = _service.IssueCertificate(csr, templateName, validity);

        // Assert
        certificate.Should().NotBeNull();
        certificate!.Subject.Should().Contain("Demo Certificate");
        certificate.NotAfter.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void IssueCertificate_ValidInput_CertificateHasCorrectValidity()
    {
        // Arrange
        var csr = _service.GenerateCertificateRequest("CN=test", 2048);
        var validity = TimeSpan.FromDays(30);

        // Act
        var certificate = _service.IssueCertificate(csr, "Test", validity);

        // Assert
        certificate.Should().NotBeNull();
        var expectedExpiry = DateTimeOffset.UtcNow.Add(validity);
        certificate!.NotAfter.Should().BeCloseTo(expectedExpiry.DateTime, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void RevokeCertificate_ValidThumbprint_ReturnsTrue()
    {
        // Arrange
        var thumbprint = "1234567890ABCDEF";
        var reason = "KeyCompromise";

        // Act
        var result = _service.RevokeCertificate(thumbprint, reason);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetCertificateByThumbprint_NonExistentThumbprint_ReturnsNull()
    {
        // Arrange
        var thumbprint = "NONEXISTENT123456";

        // Act
        var certificate = _service.GetCertificateByThumbprint(thumbprint);

        // Assert
        certificate.Should().BeNull();
    }

    [Fact]
    public void GetCertificatesBySubject_NonExistentSubject_ReturnsEmpty()
    {
        // Arrange
        var subjectName = "CN=nonexistent.example.com";

        // Act
        var certificates = _service.GetCertificatesBySubject(subjectName);

        // Assert
        certificates.Should().BeEmpty();
    }
}
