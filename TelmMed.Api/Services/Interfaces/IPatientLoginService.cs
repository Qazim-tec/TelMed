// Services/Interfaces/IPatientLoginService.cs
namespace TelmMed.Api.Services.Interfaces
{
    public interface IPatientLoginService
    {
        Task<string> LoginWithPinAsync(string phoneNumber, string pin);
        Task RequestPinResetAsync(string phoneNumber);
        Task<string> ResetPinWithOtpAsync(string phoneNumber, string firebaseIdToken, string newPin);
    }
}