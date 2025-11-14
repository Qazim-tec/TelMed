namespace TelmMed.Api.DTOs
{
    public record LanguagePreferencesRequestDto(
     string PreferredLanguage,
     string? AlternativeLanguage,
     string CommunicationTone,
     List<string> CommunicationChannels
 );
}
