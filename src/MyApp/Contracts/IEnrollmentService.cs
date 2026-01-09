using System.ServiceModel;
using MyApp.Models;

namespace MyApp.Contracts;

[ServiceContract(Namespace = "http://demo.enrollment.services/")]
public interface IEnrollmentService
{
    [OperationContract]
    EnrollmentResponse RequestCertificate(EnrollmentRequest request);

    [OperationContract]
    CertificateStatus GetCertificateStatus(string requestId);

    [OperationContract]
    EnrollmentResponse RenewCertificate(string thumbprint);

    [OperationContract]
    bool RevokeCertificate(string thumbprint, string reason);
}
