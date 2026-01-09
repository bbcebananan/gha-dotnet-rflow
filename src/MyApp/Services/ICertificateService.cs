using System.Security.Cryptography.X509Certificates;

namespace MyApp.Services;

public interface ICertificateService
{
    string GenerateCertificateRequest(string subjectName, int keyLength);

    X509Certificate2? IssueCertificate(string csr, string templateName, TimeSpan validity);

    bool RevokeCertificate(string thumbprint, string reason);

    X509Certificate2? GetCertificateByThumbprint(string thumbprint);

    IEnumerable<X509Certificate2> GetCertificatesBySubject(string subjectName);

    byte[] SignData(byte[] data, string thumbprint);

    bool VerifySignature(byte[] data, byte[] signature, string thumbprint);
}
