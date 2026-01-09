using System.Runtime.Serialization;

namespace MyApp.Models;

[DataContract(Namespace = "http://demo.enrollment.services/")]
public class EnrollmentResponse
{
    [DataMember(Order = 1)]
    public string? RequestId { get; set; }

    [DataMember(Order = 2)]
    public EnrollmentStatus Status { get; set; }

    [DataMember(Order = 3)]
    public string? Certificate { get; set; }

    [DataMember(Order = 4)]
    public string? Message { get; set; }

    [DataMember(Order = 5)]
    public DateTime? IssuedAt { get; set; }

    [DataMember(Order = 6)]
    public DateTime? ExpiresAt { get; set; }

    [DataMember(Order = 7)]
    public string? Thumbprint { get; set; }
}

[DataContract(Namespace = "http://demo.enrollment.services/")]
public enum EnrollmentStatus
{
    [EnumMember]
    Pending = 0,

    [EnumMember]
    Issued = 1,

    [EnumMember]
    Denied = 2,

    [EnumMember]
    Failed = 3,

    [EnumMember]
    Revoked = 4
}
