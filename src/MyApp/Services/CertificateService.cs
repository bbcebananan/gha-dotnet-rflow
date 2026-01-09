using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace MyApp.Services;

public class CertificateService : ICertificateService
{
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(ILogger<CertificateService> logger)
    {
        _logger = logger;
    }

    public string GenerateCertificateRequest(string subjectName, int keyLength)
    {
        _logger.LogInformation("Generating certificate request for {SubjectName} with key length {KeyLength}",
          subjectName, keyLength);

        using var rsa = RSA.Create(keyLength);
        var request = new CertificateRequest(
          subjectName,
          rsa,
          HashAlgorithmName.SHA256,
          RSASignaturePadding.Pkcs1);

        // Add key usage extension
        request.CertificateExtensions.Add(
          new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));

        // Generate PKCS#10 CSR
        var csr = request.CreateSigningRequest();
        var base64Csr = Convert.ToBase64String(csr);

        _logger.LogDebug("Generated CSR of length {Length}", base64Csr.Length);

        return base64Csr;
    }

    public X509Certificate2? IssueCertificate(string csr, string templateName, TimeSpan validity)
    {
        _logger.LogInformation("Issuing certificate for template {TemplateName} with validity {Validity}",
          templateName, validity);

        // Demo implementation - creates a self-signed certificate
        // In production, this would submit to a CA
        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
              "CN=Demo Certificate",
              rsa,
              HashAlgorithmName.SHA256,
              RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
              new X509BasicConstraintsExtension(false, false, 0, true));

            request.CertificateExtensions.Add(
              new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                true));

            var certificate = request.CreateSelfSigned(
              DateTimeOffset.UtcNow,
              DateTimeOffset.UtcNow.Add(validity));

            _logger.LogInformation("Issued certificate with thumbprint {Thumbprint}",
              certificate.Thumbprint);

            return certificate;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to issue certificate");
            return null;
        }
    }

    public bool RevokeCertificate(string thumbprint, string reason)
    {
        _logger.LogInformation("Revoking certificate {Thumbprint} for reason: {Reason}",
          thumbprint, reason);

        // Demo implementation - in production would update CRL
        return true;
    }

    public X509Certificate2? GetCertificateByThumbprint(string thumbprint)
    {
        _logger.LogDebug("Looking up certificate by thumbprint {Thumbprint}", thumbprint);

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(
          X509FindType.FindByThumbprint,
          thumbprint,
          validOnly: false);

        return certificates.Count > 0 ? certificates[0] : null;
    }

    public IEnumerable<X509Certificate2> GetCertificatesBySubject(string subjectName)
    {
        _logger.LogDebug("Looking up certificates by subject {SubjectName}", subjectName);

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(
          X509FindType.FindBySubjectDistinguishedName,
          subjectName,
          validOnly: false);

        return certificates.ToList();
    }

    public byte[] SignData(byte[] data, string thumbprint)
    {
        _logger.LogDebug("Signing data with certificate {Thumbprint}", thumbprint);

        var certificate = GetCertificateByThumbprint(thumbprint)
          ?? throw new InvalidOperationException($"Certificate not found: {thumbprint}");

        var contentInfo = new ContentInfo(data);
        var signedCms = new SignedCms(contentInfo);
        var signer = new CmsSigner(certificate);

        signedCms.ComputeSignature(signer);

        return signedCms.Encode();
    }

    public bool VerifySignature(byte[] data, byte[] signature, string thumbprint)
    {
        _logger.LogDebug("Verifying signature with certificate {Thumbprint}", thumbprint);

        try
        {
            var signedCms = new SignedCms();
            signedCms.Decode(signature);
            signedCms.CheckSignature(verifySignatureOnly: true);
            return true;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Signature verification failed");
            return false;
        }
    }
}
