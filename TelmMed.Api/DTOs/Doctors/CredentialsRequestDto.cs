namespace TelmMed.Api.DTOs.Doctors
{
    public record CredentialsRequestDto(
    IFormFile MedicalLicense,
    IFormFile MdcnCertificate,
    IFormFile? Cv,
    IFormFile? NinSlip,
    IFormFile? Passport,
    IFormFile? AdditionalCertificate
);
}
