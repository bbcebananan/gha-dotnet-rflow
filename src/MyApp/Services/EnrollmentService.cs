using FluentValidation;
using Microsoft.Extensions.Logging;
using MyApp.Contracts;
using MyApp.Models;

namespace MyApp.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ICertificateService _certificateService;
    private readonly IValidator<EnrollmentRequest> _validator;
    private readonly ILogger<EnrollmentService> _logger;

    // In-memory storage for demo purposes
    private static readonly Dictionary<string, EnrollmentResponse> _requests = new();

    public EnrollmentService(
      ICertificateService certificateService,
      IValidator<EnrollmentRequest> validator,
      ILogger<EnrollmentService> logger)
    {
        _certificateService = certificateService;
        _validator = validator;
        _logger = logger;
    }

    public EnrollmentResponse RequestCertificate(EnrollmentRequest request)
    {
        _logger.LogInformation("Processing certificate enrollment request for {SubjectName}",
          request.SubjectName);

        // Validate the request
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed for enrollment request: {Errors}", errors);

            return new EnrollmentResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Status = EnrollmentStatus.Failed,
                Message = $"Validation failed: {errors}"
            };
        }

        try
        {
            // Generate or use provided CSR
            var csr = request.CertificateSigningRequest
              ?? _certificateService.GenerateCertificateRequest(
                  request.SubjectName!,
                  request.KeyLength);

            // Issue the certificate
            var certificate = _certificateService.IssueCertificate(
              csr,
              request.TemplateName!,
              TimeSpan.FromDays(365));

            if (certificate == null)
            {
                return new EnrollmentResponse
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Status = EnrollmentStatus.Failed,
                    Message = "Failed to issue certificate"
                };
            }

            var response = new EnrollmentResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Status = EnrollmentStatus.Issued,
                Certificate = Convert.ToBase64String(certificate.RawData),
                Thumbprint = certificate.Thumbprint,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = certificate.NotAfter,
                Message = "Certificate issued successfully"
            };

            // Store for later lookup
            _requests[response.RequestId] = response;

            _logger.LogInformation("Certificate issued successfully with thumbprint {Thumbprint}",
              certificate.Thumbprint);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing certificate enrollment request");

            return new EnrollmentResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Status = EnrollmentStatus.Failed,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public CertificateStatus GetCertificateStatus(string requestId)
    {
        _logger.LogDebug("Getting status for request {RequestId}", requestId);

        if (_requests.TryGetValue(requestId, out var response))
        {
            return new CertificateStatus
            {
                RequestId = requestId,
                Status = response.Status,
                Thumbprint = response.Thumbprint,
                NotBefore = response.IssuedAt,
                NotAfter = response.ExpiresAt,
                LastChecked = DateTime.UtcNow
            };
        }

        return new CertificateStatus
        {
            RequestId = requestId,
            Status = EnrollmentStatus.Failed,
            LastChecked = DateTime.UtcNow
        };
    }

    public EnrollmentResponse RenewCertificate(string thumbprint)
    {
        _logger.LogInformation("Processing certificate renewal for {Thumbprint}", thumbprint);

        var existingCert = _certificateService.GetCertificateByThumbprint(thumbprint);
        if (existingCert == null)
        {
            return new EnrollmentResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Status = EnrollmentStatus.Failed,
                Message = "Certificate not found"
            };
        }

        // Create renewal request based on existing certificate
        var request = new EnrollmentRequest
        {
            SubjectName = existingCert.Subject,
            TemplateName = "RenewalTemplate",
            RequestorName = "System",
            KeyLength = 2048
        };

        return RequestCertificate(request);
    }

    public bool RevokeCertificate(string thumbprint, string reason)
    {
        _logger.LogInformation("Revoking certificate {Thumbprint} for reason: {Reason}",
          thumbprint, reason);

        return _certificateService.RevokeCertificate(thumbprint, reason);
    }
}
