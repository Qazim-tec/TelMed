namespace TelmMed.Api.DTOs.Doctors
{
    public record PracticeProfileRequestDto(
    string Specialty,
    List<LanguageDto> Languages,
    string CurrentWorkplace,
    string ShortBio
);

    public record LanguageDto(
        string Name,
        string Proficiency // Basic, Intermediate, Fluent, Native
    );
}
