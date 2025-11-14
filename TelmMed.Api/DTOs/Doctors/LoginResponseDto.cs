namespace TelmMed.Api.DTOs.Doctors
{
    public record LoginResponseDto(
    Guid DoctorId,
    string PhoneNumber,
    string JwtToken,
    bool BiometricEnabled,
    string Role = "Doctor"
);

}
