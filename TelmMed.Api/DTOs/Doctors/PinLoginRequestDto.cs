namespace TelmMed.Api.DTOs.Doctors
{
    public record PinLoginRequestDto(string Pin, bool UseBiometric = false);

}
