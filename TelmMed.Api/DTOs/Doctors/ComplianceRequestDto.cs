namespace TelmMed.Api.DTOs.Doctors
{
    public record ComplianceRequestDto(
    bool AcceptTerms,
    bool AcceptPrivacy,
    bool AcceptDataUse,
    bool AcceptTelemedicine
);
}
