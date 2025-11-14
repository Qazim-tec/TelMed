using TelmMed.Api.DTOs.Doctors;

namespace TelmMed.Api.Services.Interfaces
{
    public interface IDoctorLoginService
    {
        Task<bool> ValidatePhoneAsync(string phoneNumber);
        Task<LoginResponseDto> VerifyPinAsync(Guid doctorId, string pin, bool useBiometric);
    }
}
