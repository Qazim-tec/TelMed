namespace TelmMed.Api.DTOs
{
    public record SecuritySetupRequestDto(
    string Pin,
    bool EnableBiometric,
    List<EmergencyContactDto> EmergencyContacts
);

    public record EmergencyContactDto(
        string Name,
        string PhoneNumber,
        string Relationship,
        bool AllowLocationTracking
    );
}
