namespace TelmMed.Api.DTOs
{
    public record VerifyPhoneResponseDto(
        Guid PatientId,
        string PhoneNumber,
        string JwtToken
    );
}
