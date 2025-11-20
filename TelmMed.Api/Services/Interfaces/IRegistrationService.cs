using TelmMed.Api.DTOs;


namespace TelmMed.Api.Services.Interfaces
{
    public interface IRegistrationService
    {
        Task<VerifyPhoneResponseDto> VerifyPhoneWithFirebaseAsync(string firebaseIdToken);
        Task SaveCategoriesAsync(Guid patientId, List<string> categories);
        Task SaveLanguagePreferencesAsync(Guid patientId, LanguagePreferencesRequestDto dto);
        Task SavePersonalInfoAsync(Guid patientId, PersonalInfoRequestDto dto);
        Task<CompleteRegistrationResponseDto> CompleteRegistrationAsync(Guid patientId, SecuritySetupRequestDto dto);
    }

    public record CompleteRegistrationResponseDto(
        Guid PatientId,
        string PhoneNumber,
        string Message
    );
}