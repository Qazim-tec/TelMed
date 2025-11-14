namespace TelmMed.Api.DTOs
{
    public record LoginResponseDto(
    Guid PatientId,
    string PhoneNumber,
    string JwtToken,
    bool BiometricEnabled
);

}
