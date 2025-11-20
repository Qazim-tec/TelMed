using System.ComponentModel.DataAnnotations;

namespace TelmMed.Api.Models
{
    public enum DoctorStatus
    {
        Pending,
        UnderReview,
        Approved,
        Rejected
    }

    public class Doctor
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Identity
        public string LegalName { get; set; } = null!;
        public string Sex { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public bool IsPhoneVerified { get; set; }
        public string Email { get; set; } = null!;
        public string? AlternatePhone { get; set; }
        public string? WorkEmail { get; set; }
        public string ResidentialAddress { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Lga { get; set; } = null!;

        // Security
        public string PinHash { get; set; } = null!;
        public bool BiometricEnabled { get; set; }

        // Practice
        public string Specialty { get; set; } = null!;
        public string CurrentWorkplace { get; set; } = null!;
        public string ShortBio { get; set; } = null!;
        public List<DoctorLanguage> Languages { get; set; } = new();

        // Documents
        public string? MedicalLicensePath { get; set; }
        public string? MdcnCertificatePath { get; set; }
        public string? CvPath { get; set; }
        public string? NinSlipPath { get; set; }
        public string? PassportPath { get; set; }
        public string? AdditionalCertificatePath { get; set; }

        // Compliance
        public bool AcceptTerms { get; set; }
        public bool AcceptPrivacy { get; set; }
        public bool AcceptDataUse { get; set; }
        public bool AcceptTelemedicine { get; set; }

        // Interview
        public DateTime? PreferredInterview { get; set; }
        public List<AlternativeInterviewSlot> AlternativeSlots { get; set; } = new();
        public string? InterviewNotes { get; set; }

        // Relations
        public NextOfKin NextOfKin { get; set; } = null!;

        // === ADMIN APPROVAL FIELDS (MUST BE INSIDE Doctor class) ===
        public DoctorStatus Status { get; set; } = DoctorStatus.Pending;
        public string? RejectionReason { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DoctorLanguage
    {
        public int Id { get; set; }
        public Guid DoctorId { get; set; }
        public string Name { get; set; } = null!;
        public string Proficiency { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
    }

    public class NextOfKin
    {
        public int Id { get; set; }
        public Guid DoctorId { get; set; }
        public string Name { get; set; } = null!;
        public string Relationship { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
    }

    public class AlternativeInterviewSlot
    {
        public int Id { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime DateTime { get; set; }
        public Doctor Doctor { get; set; } = null!;
    }
}