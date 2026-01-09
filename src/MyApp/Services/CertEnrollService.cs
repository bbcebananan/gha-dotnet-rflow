using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CERTENROLLLib;
using Microsoft.Extensions.Logging;

namespace MyApp.Services;

/// <summary>
/// Service implementation for Windows Certificate Enrollment using CertEnroll COM API.
/// This demonstrates usage of the Interop.CERTENROLLLib package.
/// Note: Most operations require Windows and may require elevated privileges or AD CS infrastructure.
/// </summary>
[SupportedOSPlatform("windows")]
public class CertEnrollService : ICertEnrollService
{
    private readonly ILogger<CertEnrollService> _logger;

    public CertEnrollService(ILogger<CertEnrollService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string CreateCertificateRequest(CertEnrollRequestOptions options)
    {
        _logger.LogInformation("Creating certificate request for subject: {SubjectName}", options.SubjectName);

        try
        {
            // Create a new private key
            var privateKey = new CX509PrivateKey();
            privateKey.ProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";
            privateKey.MachineContext = false;
            privateKey.Length = options.KeyLength;
            privateKey.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE;
            privateKey.ExportPolicy = options.Exportable
              ? X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG
              : X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_NONE;
            privateKey.Create();

            // Create the PKCS#10 certificate request
            var pkcs10 = new CX509CertificateRequestPkcs10();
            pkcs10.InitializeFromPrivateKey(
              X509CertificateEnrollmentContext.ContextUser,
              privateKey,
              string.Empty);

            // Set the subject name
            var subjectDN = new CX500DistinguishedName();
            subjectDN.Encode(options.SubjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);
            pkcs10.Subject = subjectDN;

            // Add key usage extension
            var keyUsageExtension = new CX509ExtensionKeyUsage();
            var keyUsageFlags = ParseKeyUsage(options.KeyUsage);
            keyUsageExtension.InitializeEncode(keyUsageFlags);
            pkcs10.X509Extensions.Add((CX509Extension)keyUsageExtension);

            // Add Subject Alternative Names if provided
            if (options.SubjectAlternativeNames?.Length > 0)
            {
                var sanExtension = CreateSanExtension(options.SubjectAlternativeNames);
                pkcs10.X509Extensions.Add((CX509Extension)sanExtension);
            }

            // Set friendly name if provided
            if (!string.IsNullOrEmpty(options.FriendlyName))
            {
                // Friendly name is set on the enrollment object, not the request
            }

            // Create enrollment request
            var enrollment = new CX509Enrollment();
            enrollment.InitializeFromRequest(pkcs10);

            if (!string.IsNullOrEmpty(options.FriendlyName))
            {
                enrollment.CertificateFriendlyName = options.FriendlyName;
            }

            // Create the request
            var csr = enrollment.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER);

            _logger.LogInformation("Certificate request created successfully");
            return csr;
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM error creating certificate request: 0x{HResult:X8}", ex.HResult);
            throw new InvalidOperationException($"Failed to create certificate request: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool InstallCertificate(string certificateResponse, string password)
    {
        _logger.LogInformation("Installing certificate from response");

        try
        {
            var enrollment = new CX509Enrollment();
            enrollment.Initialize(X509CertificateEnrollmentContext.ContextUser);

            // Install the certificate response
            enrollment.InstallResponse(
              InstallResponseRestrictionFlags.AllowUntrustedCertificate,
              certificateResponse,
              EncodingType.XCN_CRYPT_STRING_BASE64,
              password);

            _logger.LogInformation("Certificate installed successfully");
            return true;
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM error installing certificate: 0x{HResult:X8}", ex.HResult);
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableTemplates()
    {
        _logger.LogInformation("Retrieving available certificate templates");

        var templates = new List<string>();

        try
        {
            // Create policy server for AD CS
            var policy = new CX509EnrollmentPolicyWebService();

            try
            {
                // Try to connect to AD policy (requires domain environment)
                policy.Initialize(
                  "ldap:",
                  null,
                  X509EnrollmentAuthFlags.X509AuthKerberos,
                  false,
                  X509CertificateEnrollmentContext.ContextUser);

                policy.LoadPolicy(X509EnrollmentPolicyLoadOption.LoadOptionDefault);

                var templateCollection = policy.GetTemplates();
                foreach (IX509CertificateTemplate template in templateCollection)
                {
                    templates.Add(template.Property[EnrollmentTemplateProperty.TemplatePropCommonName] as string ?? "Unknown");
                }
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80070035)) // Network path not found
            {
                _logger.LogWarning("AD CS policy server not available. Returning demo templates.");
                // Return demo templates when not in AD environment
                templates.AddRange(["WebServer", "CodeSigning", "User", "Computer", "DomainController"]);
            }
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM error retrieving templates: 0x{HResult:X8}", ex.HResult);
            // Return demo templates on error
            templates.AddRange(["WebServer", "CodeSigning", "User", "Computer"]);
        }

        _logger.LogInformation("Found {Count} certificate templates", templates.Count);
        return templates;
    }

    /// <inheritdoc />
    public string CreateSelfSignedCertificate(string subjectName, int validityDays)
    {
        _logger.LogInformation("Creating self-signed certificate for: {SubjectName}", subjectName);

        try
        {
            // Create private key
            var privateKey = new CX509PrivateKey();
            privateKey.ProviderName = "Microsoft Enhanced RSA and AES Cryptographic Provider";
            privateKey.MachineContext = false;
            privateKey.Length = 2048;
            privateKey.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE;
            privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;
            privateKey.Create();

            // Create self-signed certificate request
            var cert = new CX509CertificateRequestCertificate();
            cert.InitializeFromPrivateKey(
              X509CertificateEnrollmentContext.ContextUser,
              privateKey,
              string.Empty);

            // Set subject and issuer (same for self-signed)
            var subjectDN = new CX500DistinguishedName();
            subjectDN.Encode(subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);
            cert.Subject = subjectDN;
            cert.Issuer = subjectDN;

            // Set validity period
            cert.NotBefore = DateTime.UtcNow;
            cert.NotAfter = DateTime.UtcNow.AddDays(validityDays);

            // Add basic constraints (CA: false)
            var basicConstraints = new CX509ExtensionBasicConstraints();
            basicConstraints.InitializeEncode(false, 0);
            basicConstraints.Critical = true;
            cert.X509Extensions.Add((CX509Extension)basicConstraints);

            // Add key usage
            var keyUsage = new CX509ExtensionKeyUsage();
            keyUsage.InitializeEncode(
              CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |
              CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE);
            cert.X509Extensions.Add((CX509Extension)keyUsage);

            // Encode the certificate
            cert.Encode();

            // Create enrollment and install
            var enrollment = new CX509Enrollment();
            enrollment.InitializeFromRequest(cert);
            enrollment.CertificateFriendlyName = $"Self-Signed: {subjectName}";

            // Create and install the certificate
            var certData = enrollment.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64);
            enrollment.InstallResponse(
              InstallResponseRestrictionFlags.AllowUntrustedCertificate,
              certData,
              EncodingType.XCN_CRYPT_STRING_BASE64,
              string.Empty);

            // Get the installed certificate thumbprint
            var installedCert = enrollment.Certificate[EncodingType.XCN_CRYPT_STRING_BASE64];
            var certBytes = Convert.FromBase64String(installedCert);
            using var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

            _logger.LogInformation("Self-signed certificate created with thumbprint: {Thumbprint}",
              x509Cert.Thumbprint);

            return x509Cert.Thumbprint;
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM error creating self-signed certificate: 0x{HResult:X8}", ex.HResult);
            throw new InvalidOperationException($"Failed to create self-signed certificate: {ex.Message}", ex);
        }
    }

    private static CERTENROLLLib.X509KeyUsageFlags ParseKeyUsage(string keyUsage)
    {
        return keyUsage.ToLowerInvariant() switch
        {
            "digitalsignature" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE,
            "keyencipherment" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE,
            "dataencipherment" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DATA_ENCIPHERMENT_KEY_USAGE,
            "keyagreement" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_AGREEMENT_KEY_USAGE,
            "keycertsign" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_CERT_SIGN_KEY_USAGE,
            "crlsign" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_CRL_SIGN_KEY_USAGE,
            "encipheronly" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_ENCIPHER_ONLY_KEY_USAGE,
            "decipheronly" => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DECIPHER_ONLY_KEY_USAGE,
            _ => CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |
                 CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE
        };
    }

    private static CX509ExtensionAlternativeNames CreateSanExtension(string[] alternativeNames)
    {
        var altNames = new CAlternativeNames();

        foreach (var name in alternativeNames)
        {
            var altName = new CAlternativeName();

            // Determine if it's an IP address or DNS name
            if (System.Net.IPAddress.TryParse(name, out _))
            {
                altName.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_IP_ADDRESS, name);
            }
            else if (name.Contains('@'))
            {
                altName.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_RFC822_NAME, name);
            }
            else
            {
                altName.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME, name);
            }

            altNames.Add(altName);
        }

        var sanExtension = new CX509ExtensionAlternativeNames();
        sanExtension.InitializeEncode(altNames);

        return sanExtension;
    }
}
