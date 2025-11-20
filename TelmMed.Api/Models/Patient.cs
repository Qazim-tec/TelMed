using System.ComponentModel.DataAnnotations;

namespace TelmMed.Api.Models
{
    public class Patient
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsPhoneVerified { get; set; } = false;

        // Step 1
        public List<string> SelectedCategories { get; set; } = new();
        public string PreferredLanguage { get; set; } = "English";
        public string? AlternativeLanguage { get; set; }
        public string CommunicationTone { get; set; } = "standard";
        public List<string> CommunicationChannels { get; set; } = new();

        // Step 2
        [Required]
        public string FirstName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string SexAtBirth { get; set; } = string.Empty;
        public List<string> MedicalConditions { get; set; } = new();
        public string? BloodGroup { get; set; }
        public string? Genotype { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }
        public bool AgreesToTerms { get; set; } = false;

        // Step 3
        [Required]
        public string PinHash { get; set; } = string.Empty;
        public bool BiometricEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Fixed: Use List<> instead of ICollection<>
        public List<EmergencyContact> EmergencyContacts { get; set; } = new();
    }
}