namespace TelmMed.Api.DTOs.Doctors
{
    public record IdentityRequestDto(
    string LegalName,
    string Sex,
    string PhoneNumber,
    string Email,
    string? AlternatePhone,
    string? WorkEmail,
    string ResidentialAddress,
    string State,
    string Lga,
    string Pin,
    bool EnableBiometric,
    NextOfKinDto NextOfKin
);

    public record NextOfKinDto(
        string Name,
        string Relationship,
        string PhoneNumber
    );
}
