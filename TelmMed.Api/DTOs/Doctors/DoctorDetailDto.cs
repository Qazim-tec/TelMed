using TelmMed.Api.Models;

namespace TelmMed.Api.DTOs.Doctors
{
    public record DoctorDetailDto(
    Guid Id,
    string LegalName,
    string Email,
    string PhoneNumber,
    string Specialty,
    string CurrentWorkplace,
    string ShortBio,
    List<string> Languages,
    string MedicalLicensePath,
    string MdcnCertificatePath,
    DoctorStatus Status,
    string? RejectionReason,
    DateTime? ReviewedAt
);
}
