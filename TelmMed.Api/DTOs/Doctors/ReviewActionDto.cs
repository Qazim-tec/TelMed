using TelmMed.Api.Models;

namespace TelmMed.Api.DTOs.Doctors
{
    public record ReviewActionDto(
    DoctorStatus Status,
    string? RejectionReason
);
}
