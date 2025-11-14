using System.ComponentModel.DataAnnotations;

namespace TelmMed.Api.Models
{
    public class EmergencyContact
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        [Required] public string Name { get; set; } = string.Empty;
        [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
        [Required] public string Relationship { get; set; } = string.Empty;
        public bool AllowLocationTracking { get; set; } = false;
    }
}