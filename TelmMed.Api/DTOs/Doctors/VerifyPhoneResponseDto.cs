namespace TelmMed.Api.DTOs.Doctors
{
    public record VerifyPhoneResponseDto(Guid DoctorId, string PhoneNumber, string JwtToken);
}
