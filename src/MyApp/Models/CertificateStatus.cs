using System.Runtime.Serialization;

namespace MyApp.Models;

[DataContract(Namespace = "http://demo.enrollment.services/")]
public class CertificateStatus
{
    [DataMember(Order = 1)]
    public string? RequestId { get; set; }

    [DataMember(Order = 2)]
    public EnrollmentStatus Status { get; set; }

    [DataMember(Order = 3)]
    public string? SubjectName { get; set; }

    [DataMember(Order = 4)]
    public string? SerialNumber { get; set; }

    [DataMember(Order = 5)]
    public DateTime? NotBefore { get; set; }

    [DataMember(Order = 6)]
    public DateTime? NotAfter { get; set; }

    [DataMember(Order = 7)]
    public string? Thumbprint { get; set; }

    [DataMember(Order = 8)]
    public string? IssuerName { get; set; }

    [DataMember(Order = 9)]
    public DateTime? LastChecked { get; set; }
}
