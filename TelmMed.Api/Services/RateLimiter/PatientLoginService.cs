// Services/PatientLoginService.cs
using BCrypt.Net;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Services
{
    public class PatientLoginService : IPatientLoginService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IRegistrationService _registrationService; // ← NOW USING INTERFACE

        public PatientLoginService(
            AppDbContext context,
            IJwtService jwtService,
            IRegistrationService registrationService) // ← INTERFACE
        {
            _context = context;
            _jwtService = jwtService;
            _registrationService = registrationService;
        }

        public async Task<string> LoginWithPinAsync(string phoneNumber, string pin)
        {
            var normalized = NormalizePhoneNumber(phoneNumber);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == normalized && p.PinHash != null);

            if (patient == null || !BCrypt.Net.BCrypt.Verify(pin, patient.PinHash))
                throw new UnauthorizedAccessException("Invalid phone number or PIN.");

            return _jwtService.GenerateToken(patient.Id, patient.PhoneNumber, "Patient");
        }

        public async Task RequestPinResetAsync(string phoneNumber)
        {
            var normalized = NormalizePhoneNumber(phoneNumber);

            var exists = await _context.Patients
                .AnyAsync(p => p.PhoneNumber == normalized && p.IsPhoneVerified && p.PinHash != null);

            if (!exists)
                throw new KeyNotFoundException("No registered patient found with this phone number.");
        }

        public async Task<string> ResetPinWithOtpAsync(string phoneNumber, string firebaseIdToken, string newPin)
        {
            if (newPin.Length != 5 || !newPin.All(char.IsDigit))
                throw new ArgumentException("New PIN must be exactly 5 digits.");

            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(firebaseIdToken);
            var otpPhone = decoded.Claims["phone_number"]?.ToString()
                ?? throw new UnauthorizedAccessException("Invalid OTP.");

            var normalizedInput = NormalizePhoneNumber(phoneNumber);
            var normalizedOtp = NormalizePhoneNumber(otpPhone);

            if (normalizedInput != normalizedOtp)
                throw new UnauthorizedAccessException("Phone number does not match OTP.");

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == normalizedInput)
                ?? throw new KeyNotFoundException("Patient not found.");

            patient.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            patient.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return _jwtService.GenerateToken(patient.Id, patient.PhoneNumber, "Patient");
        }

        // Reuse the exact same logic from RegistrationService
        private string NormalizePhoneNumber(string phone)
        {
            var trimmed = phone.Trim();
            return trimmed.StartsWith("+") ? trimmed : "+234" + trimmed.TrimStart('0');
        }
    }
}