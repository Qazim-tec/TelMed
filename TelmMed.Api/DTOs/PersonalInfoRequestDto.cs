namespace TelmMed.Api.DTOs
{
    public record PersonalInfoRequestDto(
    string FirstName,
    DateTime DateOfBirth,
    string SexAtBirth,
    List<string> MedicalConditions,
    string? BloodGroup,
    string? Genotype,
    string? Allergies,
    string? CurrentMedications,
    bool AgreesToTerms
);
}
