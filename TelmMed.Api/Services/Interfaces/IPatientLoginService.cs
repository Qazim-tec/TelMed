using TelmMed.Api.DTOs;

namespace TelmMed.Api.Services.Interfaces
{
    public interface IPatientLoginService
    {
        Task<bool> ValidatePhoneAsync(string phoneNumber);
        Task<LoginResponseDto> VerifyPinAsync(Guid patientId, string pin, bool useBiometric);
    }
}
