// DTOs/PatientAuthDto.cs
namespace TelmMed.Api.DTOs
{
    public class PatientLoginRequestDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class ForgotPinRequestDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ResetPinWithOtpDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirebaseIdToken { get; set; } = string.Empty;
        public string NewPin { get; set; } = string.Empty;
    }
}