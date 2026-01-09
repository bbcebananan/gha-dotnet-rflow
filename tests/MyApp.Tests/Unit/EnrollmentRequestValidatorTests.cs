using FluentAssertions;
using MyApp.Models;
using MyApp.Validators;
using Xunit;

namespace MyApp.Tests.Unit;

public class EnrollmentRequestValidatorTests
{
    private readonly EnrollmentRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingSubjectName_ShouldFail()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubjectName");
    }

    [Fact]
    public void Validate_InvalidSubjectName_ShouldFail()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "invalid-dn",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
          e.PropertyName == "SubjectName" &&
          e.ErrorMessage.Contains("distinguished name"));
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void Validate_ValidKeyLength_ShouldPass(int keyLength)
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = keyLength
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(512)]
    [InlineData(3072)]
    [InlineData(8192)]
    public void Validate_InvalidKeyLength_ShouldFail(int keyLength)
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = keyLength
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "KeyLength");
    }

    [Fact]
    public void Validate_MissingTemplateName_ShouldFail()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            RequestorName = "TestUser",
            KeyLength = 2048
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public void Validate_ValidKeyUsage_ShouldPass()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048,
            KeyUsage = "DigitalSignature"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidKeyUsage_ShouldFail()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048,
            KeyUsage = "InvalidUsage"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "KeyUsage");
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("192.168.1.1")]
    [InlineData("test.subdomain.example.com")]
    public void Validate_ValidSubjectAlternativeNames_ShouldPass(string san)
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048,
            SubjectAlternativeNames = [san]
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid..dns")]
    public void Validate_InvalidSubjectAlternativeNames_ShouldFail(string san)
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            SubjectName = "CN=test.example.com",
            TemplateName = "WebServer",
            RequestorName = "TestUser",
            KeyLength = 2048,
            SubjectAlternativeNames = [san]
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
