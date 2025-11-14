using TelmMed.Api.Models;

namespace TelmMed.Api.DTOs.Doctors
{
    public record DoctorListItemDto(
        Guid Id,
        string LegalName,
        string PhoneNumber,
        string Specialty,
        DoctorStatus Status,
        DateTime CreatedAt
    );
}
