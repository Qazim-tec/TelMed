// Services/RateLimiter/PatientLoginService.cs
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data;
using TelmMed.Api.DTOs;
using TelmMed.Api.Models;
using TelmMed.Api.Services.Interfaces;
using TelmMed.Api.Services.RateLimiter;

namespace TelmMed.Api.Services.RateLimiter
{
    public class PatientLoginService : IPatientLoginService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IRateLimiterService _rateLimiter;
        private readonly IConfiguration _config;

        public PatientLoginService(
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

        public async Task<bool> ValidatePhoneAsync(string phoneNumber)
        {
            var normalized = NormalizePhone(phoneNumber);

            var allowed = await _rateLimiter.IsAllowedAsync(
                $"login-phone:{normalized}",
                _config.GetValue<int>("RateLimiting:Login:MaxAttempts", 10),
                _config.GetValue<int>("RateLimiting:Login:WindowSeconds", 900));

            if (!allowed)
                throw new UnauthorizedAccessException("Too many login attempts. Try again later.");

            return await _context.Patients
                .AnyAsync(p => p.PhoneNumber == normalized && p.IsPhoneVerified);
        }

        public async Task<LoginResponseDto> VerifyPinAsync(Guid patientId, string pin, bool useBiometric = false)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.IsPhoneVerified)
                ?? throw new UnauthorizedAccessException("Patient not found or phone not verified.");

            var loginKey = $"login-pin:{patientId}";

            // Rate limit per patient
            var allowed = await _rateLimiter.IsAllowedAsync(loginKey, 5, 300);
            if (!allowed)
                throw new UnauthorizedAccessException("Too many failed attempts. Try again in 5 minutes.");

            bool pinValid = !string.IsNullOrEmpty(patient.PinHash) &&
                           BCrypt.Net.BCrypt.Verify(pin, patient.PinHash);

            // Biometric: Skip PIN if enabled and requested
            if (useBiometric && patient.BiometricEnabled)
            {
                // In production: Validate biometric signature from client
                // For now: Trust if enabled
                await _rateLimiter.ResetAsync(loginKey); // Success
            }
            else if (!pinValid)
            {
                await _rateLimiter.RecordFailureAsync(loginKey);
                throw new UnauthorizedAccessException("Incorrect PIN.");
            }
            else
            {
                await _rateLimiter.ResetAsync(loginKey); // Success
            }

            // FIXED: Role "Patient" is REQUIRED
            var token = _jwtService.GenerateToken(patient.Id, patient.PhoneNumber, "Patient");

            return new LoginResponseDto(
                patient.Id,
                patient.PhoneNumber,
                token,
                patient.BiometricEnabled
            );
        }

        public static string NormalizePhone(string phone)
        {
            var trimmed = phone.Trim();
            return trimmed.StartsWith("+") ? trimmed : "+234" + trimmed.TrimStart('0');
        }
    }
}