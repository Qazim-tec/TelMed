// Services/Interfaces/IDoctorRegistrationService.cs
using TelmMed.Api.DTOs.Doctors;

namespace TelmMed.Api.Services.Interfaces
{
    public interface IDoctorRegistrationService
    {
        Task<VerifyPhoneResponseDto> VerifyPhoneAsync(string firebaseIdToken);
        Task SaveIdentityAsync(Guid doctorId, IdentityRequestDto dto);
        Task SavePracticeAsync(Guid doctorId, PracticeProfileRequestDto dto);
        Task SaveCredentialsAsync(Guid doctorId, CredentialsRequestDto dto);
        Task SaveComplianceAsync(Guid doctorId, ComplianceRequestDto dto);
        Task SaveScheduleAsync(Guid doctorId, ScheduleRequestDto dto);
        Task<DoctorCompleteResponseDto> CompleteRegistrationAsync(Guid doctorId);
    }



    public record DoctorCompleteResponseDto(
        Guid DoctorId,
        string PhoneNumber,
        string Message
    );
}