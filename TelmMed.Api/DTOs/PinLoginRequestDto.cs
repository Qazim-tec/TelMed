namespace TelmMed.Api.DTOs
{
    public record PinLoginRequestDto(string Pin, bool UseBiometric = false);

}
