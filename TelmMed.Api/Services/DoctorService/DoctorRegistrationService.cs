// Services/DoctorService/DoctorRegistrationService.cs
using BCrypt.Net;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data;
using TelmMed.Api.DTOs;
using TelmMed.Api.DTOs.Doctors;
using TelmMed.Api.Models;
using TelmMed.Api.Services.Interfaces;
using TelmMed.Api.Services.RateLimiter;
using VerifyPhoneResponseDto = TelmMed.Api.DTOs.Doctors.VerifyPhoneResponseDto;

namespace TelmMed.Api.Services.DoctorService
{
    public class DoctorRegistrationService : IDoctorRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IRateLimiterService _rateLimiter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public DoctorRegistrationService(
            AppDbContext context,
            IJwtService jwtService,
            IRateLimiterService rateLimiter,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _context = context;
            _jwtService = jwtService;
            _rateLimiter = rateLimiter;
            _env = env;
            _config = config;
        }

        public async Task<VerifyPhoneResponseDto> VerifyPhoneAsync(string firebaseIdToken)
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(firebaseIdToken);
            var phone = decoded.Claims["phone_number"]?.ToString()
                ?? throw new UnauthorizedAccessException("Phone missing.");

            var normalized = NormalizePhone(phone);

            var allowed = await _rateLimiter.IsAllowedAsync($"doc-verify:{normalized}", 5, 3600);
            if (!allowed) throw new UnauthorizedAccessException("Too many attempts.");

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.PhoneNumber == normalized);

            if (doctor == null)
            {
                doctor = new Doctor
                {
                    PhoneNumber = normalized,
                    IsPhoneVerified = true,
                    Status = DoctorStatus.Pending
                };
                _context.Doctors.Add(doctor);
            }
            else
            {
                doctor.IsPhoneVerified = true;
            }

            await _context.SaveChangesAsync();

            // FIXED: Role "Doctor" is REQUIRED
            var jwt = _jwtService.GenerateToken(doctor.Id, doctor.PhoneNumber, "Doctor");

            return new VerifyPhoneResponseDto(doctor.Id, doctor.PhoneNumber, jwt);
        }

        public async Task SaveIdentityAsync(Guid doctorId, IdentityRequestDto dto)
        {
            var doc = await GetDoctorAsync(doctorId);
            doc.LegalName = dto.LegalName;
            doc.Sex = dto.Sex;
            doc.Email = dto.Email;
            doc.AlternatePhone = dto.AlternatePhone;
            doc.WorkEmail = dto.WorkEmail;
            doc.ResidentialAddress = dto.ResidentialAddress;
            doc.State = dto.State;
            doc.Lga = dto.Lga;
            doc.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            doc.BiometricEnabled = dto.EnableBiometric;

            doc.NextOfKin = new NextOfKin
            {
                Name = dto.NextOfKin.Name,
                Relationship = dto.NextOfKin.Relationship,
                PhoneNumber = NormalizePhone(dto.NextOfKin.PhoneNumber)
            };

            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SavePracticeAsync(Guid doctorId, PracticeProfileRequestDto dto)
        {
            var doc = await GetDoctorAsync(doctorId);
            doc.Specialty = dto.Specialty;
            doc.CurrentWorkplace = dto.CurrentWorkplace;
            doc.ShortBio = dto.ShortBio;

            doc.Languages.Clear();
            foreach (var lang in dto.Languages)
            {
                doc.Languages.Add(new DoctorLanguage
                {
                    Name = lang.Name,
                    Proficiency = lang.Proficiency
                });
            }

            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveCredentialsAsync(Guid doctorId, CredentialsRequestDto dto)
        {
            var doc = await GetDoctorAsync(doctorId);
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "doctors", doctorId.ToString());
            Directory.CreateDirectory(uploadPath);

            doc.MedicalLicensePath = await SaveFile(dto.MedicalLicense, uploadPath, "license");
            doc.MdcnCertificatePath = await SaveFile(dto.MdcnCertificate, uploadPath, "mdcn");
            doc.CvPath = await SaveFile(dto.Cv, uploadPath, "cv");
            doc.NinSlipPath = await SaveFile(dto.NinSlip, uploadPath, "nin");
            doc.PassportPath = await SaveFile(dto.Passport, uploadPath, "passport");
            doc.AdditionalCertificatePath = await SaveFile(dto.AdditionalCertificate, uploadPath, "extra");

            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveComplianceAsync(Guid doctorId, ComplianceRequestDto dto)
        {
            var doc = await GetDoctorAsync(doctorId);
            doc.AcceptTerms = dto.AcceptTerms;
            doc.AcceptPrivacy = dto.AcceptPrivacy;
            doc.AcceptDataUse = dto.AcceptDataUse;
            doc.AcceptTelemedicine = dto.AcceptTelemedicine;
            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveScheduleAsync(Guid doctorId, ScheduleRequestDto dto)
        {
            var doc = await GetDoctorAsync(doctorId);
            doc.PreferredInterview = dto.PreferredDate.Date + dto.PreferredTime;
            doc.InterviewNotes = dto.Notes;

            doc.AlternativeSlots.Clear();
            foreach (var alt in dto.Alternatives)
            {
                doc.AlternativeSlots.Add(new AlternativeInterviewSlot
                {
                    DateTime = alt.Date.Date + alt.Time
                });
            }

            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<DoctorCompleteResponseDto> CompleteRegistrationAsync(Guid doctorId)
        {
            var doc = await GetDoctorAsync(doctorId);
            // Optional: Trigger email, notification, etc.
            return new DoctorCompleteResponseDto(doc.Id, doc.PhoneNumber, "Doctor registration completed. Awaiting admin approval.");
        }

        private async Task<Doctor> GetDoctorAsync(Guid id)
        {
            return await _context.Doctors
                .Include(d => d.Languages)
                .Include(d => d.NextOfKin)
                .Include(d => d.AlternativeSlots)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new KeyNotFoundException("Doctor not found.");
        }

        private static string NormalizePhone(string phone)
        {
            var trimmed = phone.Trim();
            return trimmed.StartsWith("+") ? trimmed : "+234" + trimmed.TrimStart('0');
        }

        private async Task<string?> SaveFile(IFormFile? file, string folderPath, string prefix)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{prefix}_{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folderPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return relative URL
            return $"/uploads/doctors/{Path.GetFileName(folderPath)}/{fileName}";
        }
    }
}