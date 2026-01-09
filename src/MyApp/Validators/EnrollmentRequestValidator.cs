using FluentValidation;
using MyApp.Models;

namespace MyApp.Validators;

public class EnrollmentRequestValidator : AbstractValidator<EnrollmentRequest>
{
    private static readonly string[] ValidKeyUsages =
    [
      "DigitalSignature",
    "KeyEncipherment",
    "DataEncipherment",
    "KeyAgreement",
    "CertificateSigning",
    "CrlSigning",
    "EncipherOnly",
    "DecipherOnly"
    ];

    private static readonly int[] ValidKeyLengths = [1024, 2048, 4096];

    public EnrollmentRequestValidator()
    {
        RuleFor(x => x.SubjectName)
          .NotEmpty()
          .WithMessage("Subject name is required")
          .Must(BeValidDistinguishedName)
          .WithMessage("Subject name must be a valid distinguished name (e.g., CN=example.com)");

        RuleFor(x => x.TemplateName)
          .NotEmpty()
          .WithMessage("Certificate template name is required")
          .MaximumLength(100)
          .WithMessage("Template name cannot exceed 100 characters");

        RuleFor(x => x.RequestorName)
          .NotEmpty()
          .WithMessage("Requestor name is required")
          .MaximumLength(256)
          .WithMessage("Requestor name cannot exceed 256 characters");

        RuleFor(x => x.KeyLength)
          .Must(length => ValidKeyLengths.Contains(length))
          .WithMessage($"Key length must be one of: {string.Join(", ", ValidKeyLengths)}");

        RuleFor(x => x.KeyUsage)
          .Must(usage => string.IsNullOrEmpty(usage) || ValidKeyUsages.Contains(usage))
          .WithMessage($"Key usage must be one of: {string.Join(", ", ValidKeyUsages)}");

        RuleForEach(x => x.SubjectAlternativeNames)
          .Must(BeValidSan)
          .WithMessage("Each subject alternative name must be a valid DNS name or IP address");
    }

    private static bool BeValidDistinguishedName(string? dn)
    {
        if (string.IsNullOrWhiteSpace(dn))
        {
            return false;
        }

        // Basic DN validation - should contain at least CN=
        return dn.Contains("CN=", StringComparison.OrdinalIgnoreCase) ||
               dn.Contains("O=", StringComparison.OrdinalIgnoreCase) ||
               dn.Contains("OU=", StringComparison.OrdinalIgnoreCase);
    }

    private static bool BeValidSan(string? san)
    {
        if (string.IsNullOrWhiteSpace(san))
        {
            return false;
        }

        // Check for valid DNS name or IP address format
        if (System.Net.IPAddress.TryParse(san, out _))
        {
            return true;
        }

        // Basic DNS name validation
        return Uri.CheckHostName(san) == UriHostNameType.Dns;
    }
}
