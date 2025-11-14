using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data;
using TelmMed.Api.DTOs.Doctors;
using TelmMed.Api.Models;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Services.DoctorService
{
    public class DoctorLoginService : IDoctorLoginService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public DoctorLoginService(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<bool> ValidatePhoneAsync(string phoneNumber)
        {
            var normalized = NormalizePhone(phoneNumber);
            return await _context.Doctors.AnyAsync(d =>
                d.PhoneNumber == normalized &&
                d.IsPhoneVerified &&
                d.Status == DoctorStatus.Approved);
        }

        public async Task<LoginResponseDto> VerifyPinAsync(Guid doctorId, string pin, bool useBiometric)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d =>
                    d.Id == doctorId &&
                    d.IsPhoneVerified &&
                    d.Status == DoctorStatus.Approved)
                ?? throw new UnauthorizedAccessException("Account not approved or invalid credentials.");

            // Biometric: Skip PIN if enabled
            if (useBiometric && doctor.BiometricEnabled)
            {
                // In real app: validate biometric signature
                // For now: trust if enabled
            }
            else
            {
                if (string.IsNullOrEmpty(doctor.PinHash) || !BCrypt.Net.BCrypt.Verify(pin, doctor.PinHash))
                    throw new UnauthorizedAccessException("Incorrect PIN.");
            }

            var token = _jwtService.GenerateToken(doctor.Id, doctor.PhoneNumber, "Doctor");
            return new LoginResponseDto(doctor.Id, doctor.PhoneNumber, token, doctor.BiometricEnabled);
        }

        public static string NormalizePhone(string phone) =>
            phone.StartsWith("+") ? phone : "+234" + phone.TrimStart('0');
    }
}
