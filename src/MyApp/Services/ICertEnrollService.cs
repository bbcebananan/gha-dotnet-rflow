namespace MyApp.Services;

/// <summary>
/// Service interface for Windows Certificate Enrollment using CertEnroll COM API.
/// This demonstrates usage of the Interop.CERTENROLLLib package.
/// </summary>
public interface ICertEnrollService
{
    /// <summary>
    /// Creates a PKCS#10 certificate signing request using CertEnroll API.
    /// </summary>
    string CreateCertificateRequest(CertEnrollRequestOptions options);

    /// <summary>
    /// Installs a certificate response from a CA.
    /// </summary>
    bool InstallCertificate(string certificateResponse, string password);

    /// <summary>
    /// Gets available certificate templates from the enrollment policy.
    /// </summary>
    IEnumerable<string> GetAvailableTemplates();

    /// <summary>
    /// Creates a self-signed certificate using CertEnroll API.
    /// </summary>
    string CreateSelfSignedCertificate(string subjectName, int validityDays);
}

public class CertEnrollRequestOptions
{
    public string SubjectName { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public int KeyLength { get; set; } = 2048;
    public string KeyUsage { get; set; } = "DigitalSignature";
    public bool Exportable { get; set; } = false;
    public string? FriendlyName { get; set; }
    public string[]? SubjectAlternativeNames { get; set; }
}
