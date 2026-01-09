using System.Runtime.Serialization;

namespace MyApp.Models;

[DataContract(Namespace = "http://demo.enrollment.services/")]
public class EnrollmentRequest
{
    [DataMember(Order = 1)]
    public string? SubjectName { get; set; }

    [DataMember(Order = 2)]
    public string? TemplateName { get; set; }

    [DataMember(Order = 3)]
    public string? RequestorName { get; set; }

    [DataMember(Order = 4)]
    public string? CertificateSigningRequest { get; set; }

    [DataMember(Order = 5)]
    public int KeyLength { get; set; } = 2048;

    [DataMember(Order = 6)]
    public string? KeyUsage { get; set; }

    [DataMember(Order = 7)]
    public string[]? SubjectAlternativeNames { get; set; }
}
