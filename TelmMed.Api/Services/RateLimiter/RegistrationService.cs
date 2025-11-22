// Services/RegistrationService.cs
using BCrypt.Net;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TelmMed.Api.Data;
using TelmMed.Api.DTOs;
using TelmMed.Api.Models;
using TelmMed.Api.Services.Interfaces;
using TelmMed.Api.Services.RateLimiter;

namespace TelmMed.Api.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IRateLimiterService _rateLimiter;
        private readonly IConfiguration _config;

        public RegistrationService(
            AppDbContext context,
            IJwtService jwtService,
            IRateLimiterService rateLimiter,
            IConfiguration config)
        {
            _context = context;
            _jwtService = jwtService;
            _rateLimiter = rateLimiter;
            _config = config;
        }

        public async Task<VerifyPhoneResponseDto> VerifyPhoneWithFirebaseAsync(string firebaseIdToken)
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(firebaseIdToken);
            var phoneNumber = decoded.Claims["phone_number"]?.ToString()
                ?? throw new UnauthorizedAccessException("Phone number not found in token.");

            var normalized = NormalizePhoneNumber(phoneNumber);

            // Rate limiting
            var max = _config.GetValue<int>("RateLimiting:VerifyPhone:MaxAttempts", 5);
            var window = _config.GetValue<int>("RateLimiting:VerifyPhone:WindowSeconds", 3600);
            var allowed = await _rateLimiter.IsAllowedAsync($"verify:{normalized}", max, window);
            if (!allowed)
                throw new UnauthorizedAccessException("Too many verification attempts.");

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == normalized);

            if (patient == null)
            {
                patient = new Patient
                {
                    PhoneNumber = normalized,
                    IsPhoneVerified = true
                };
                _context.Patients.Add(patient);
            }
            else
            {
                patient.IsPhoneVerified = true;
                patient.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var jwt = _jwtService.GenerateToken(patient.Id, patient.PhoneNumber, "Patient");
            return new VerifyPhoneResponseDto(patient.Id, patient.PhoneNumber, jwt);
        }

        public async Task SaveCategoriesAsync(Guid patientId, List<string> categories)
        {
            var p = await GetPatientAsync(patientId);
            p.SelectedCategories = categories.Count >= 3 ? categories : new List<string> { "General Care" };
            p.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveLanguagePreferencesAsync(Guid patientId, LanguagePreferencesRequestDto dto)
        {
            var p = await GetPatientAsync(patientId);
            p.PreferredLanguage = dto.PreferredLanguage;
            p.AlternativeLanguage = dto.AlternativeLanguage;
            p.CommunicationTone = dto.CommunicationTone;
            p.CommunicationChannels = dto.CommunicationChannels;
            p.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SavePersonalInfoAsync(Guid patientId, PersonalInfoRequestDto dto)
        {
            var p = await GetPatientAsync(patientId);
            p.FirstName = dto.FirstName;
            p.DateOfBirth = dto.DateOfBirth;
            p.SexAtBirth = dto.SexAtBirth;
            p.MedicalConditions = dto.MedicalConditions;
            p.BloodGroup = dto.BloodGroup;
            p.Genotype = dto.Genotype;
            p.Allergies = dto.Allergies;
            p.CurrentMedications = dto.CurrentMedications;
            p.AgreesToTerms = dto.AgreesToTerms;
            p.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // FINAL FIXED VERSION – NO MORE CONCURRENCY EXCEPTION
        public async Task<CompleteRegistrationResponseDto> CompleteRegistrationAsync(
            Guid patientId, SecuritySetupRequestDto dto)
        {
            if (dto.Pin.Length != 5 || !dto.Pin.All(char.IsDigit))
                throw new ArgumentException("PIN must be exactly 5 digits.");

            var patient = await GetPatientAsync(patientId); // Includes EmergencyContacts – kept for future edits

            patient.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            patient.BiometricEnabled = dto.EnableBiometric;
            patient.UpdatedAt = DateTime.UtcNow;

            // CRITICAL FIX: Properly handle emergency contacts during registration
            // Remove any existing (should be none, but safe)
            foreach (var contact in patient.EmergencyContacts.ToList())
            {
                _context.EmergencyContacts.Remove(contact);
            }
            patient.EmergencyContacts.Clear();

            // Add new contacts with explicit PatientId and correct EntityState
            foreach (var ec in dto.EmergencyContacts)
            {
                var emergencyContact = new EmergencyContact
                {
                    PatientId = patientId, // Explicit FK
                    Name = ec.Name,
                    PhoneNumber = NormalizePhoneNumber(ec.PhoneNumber),
                    Relationship = ec.Relationship,
                    AllowLocationTracking = ec.AllowLocationTracking
                };

                // This ensures EF knows it's a new record
                _context.Entry(emergencyContact).State = EntityState.Added;
                patient.EmergencyContacts.Add(emergencyContact);
            }

            await _context.SaveChangesAsync();

            return new CompleteRegistrationResponseDto(
                patient.Id,
                patient.PhoneNumber,
                "Registration completed successfully!"
            );
        }

        private async Task<Patient> GetPatientAsync(Guid id)
        {
            return await _context.Patients
                .Include(p => p.EmergencyContacts)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException("Patient not found.");
        }

        private static string NormalizePhoneNumber(string phone)
        {
            var trimmed = phone.Trim();
            return trimmed.StartsWith("+") ? trimmed : "+234" + trimmed.TrimStart('0');
        }
    }
}