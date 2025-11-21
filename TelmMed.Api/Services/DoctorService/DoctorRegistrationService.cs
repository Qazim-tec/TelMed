// Services/DoctorService/DoctorRegistrationService.cs
using BCrypt.Net;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data;
using TelmMed.Api.DTOs.Doctors;
using TelmMed.Api.Models;
using TelmMed.Api.Services.Interfaces;
using TelmMed.Api.Services.RateLimiter;

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
            var phoneNumber = decoded.Claims["phone_number"]?.ToString()
                ?? throw new UnauthorizedAccessException("Phone number not found in token.");

            var normalized = NormalizePhoneNumber(phoneNumber);

            var max = _config.GetValue<int>("RateLimiting:VerifyPhone:MaxAttempts", 5);
            var window = _config.GetValue<int>("RateLimiting:VerifyPhone:WindowSeconds", 3600);
            var allowed = await _rateLimiter.IsAllowedAsync($"doc-verify:{normalized}", max, window);
            if (!allowed)
                throw new UnauthorizedAccessException("Too many verification attempts.");

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.PhoneNumber == normalized);

            if (doctor == null)
            {
                doctor = new Doctor
                {
                    PhoneNumber = normalized,
                    IsPhoneVerified = true,
                    Status = DoctorStatus.Pending,

                    // Temporary values for all NOT NULL fields
                    LegalName = "Pending Registration",
                    Sex = "Other",
                    Email = $"temp_{Guid.NewGuid():N}@doctor.pending",
                    ResidentialAddress = "Pending",
                    State = "Pending",
                    Lga = "Pending",
                    PinHash = BCrypt.Net.BCrypt.HashPassword("00000"),
                    Specialty = "General Practice",
                    CurrentWorkplace = "Pending",
                    ShortBio = "Registration in progress...",

                    AcceptTerms = false,
                    AcceptPrivacy = false,
                    AcceptDataUse = false,
                    AcceptTelemedicine = false,
                    BiometricEnabled = false,

                    Languages = new List<DoctorLanguage>(),
                    AlternativeSlots = new List<AlternativeInterviewSlot>(),
                    NextOfKin = new NextOfKin
                    {
                        Name = "Pending",
                        Relationship = "Pending",
                        PhoneNumber = "+2340000000000"
                    },

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Doctors.Add(doctor);
            }
            else
            {
                doctor.IsPhoneVerified = true;
                doctor.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var jwt = _jwtService.GenerateToken(doctor.Id, doctor.PhoneNumber, "Doctor");
            return new VerifyPhoneResponseDto(doctor.Id, doctor.PhoneNumber, jwt);
        }

        public async Task SaveIdentityAsync(Guid doctorId, IdentityRequestDto dto)
        {
            var doctor = await GetDoctorAsync(doctorId);

            doctor.LegalName = dto.LegalName;
            doctor.Sex = dto.Sex;
            doctor.Email = dto.Email;
            doctor.AlternatePhone = dto.AlternatePhone;
            doctor.WorkEmail = dto.WorkEmail;
            doctor.ResidentialAddress = dto.ResidentialAddress;
            doctor.State = dto.State;
            doctor.Lga = dto.Lga;
            doctor.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            doctor.BiometricEnabled = dto.EnableBiometric;

            if (doctor.NextOfKin == null) doctor.NextOfKin = new NextOfKin();
            doctor.NextOfKin.Name = dto.NextOfKin.Name;
            doctor.NextOfKin.Relationship = dto.NextOfKin.Relationship;
            doctor.NextOfKin.PhoneNumber = NormalizePhoneNumber(dto.NextOfKin.PhoneNumber);

            doctor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SavePracticeAsync(Guid doctorId, PracticeProfileRequestDto dto)
        {
            var doctor = await GetDoctorAsync(doctorId);

            doctor.Specialty = dto.Specialty;
            doctor.CurrentWorkplace = dto.CurrentWorkplace;
            doctor.ShortBio = dto.ShortBio;

            doctor.Languages.Clear();
            foreach (var lang in dto.Languages)
            {
                doctor.Languages.Add(new DoctorLanguage { Name = lang.Name, Proficiency = lang.Proficiency });
            }

            doctor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveCredentialsAsync(Guid doctorId, CredentialsRequestDto dto)
        {
            var doctor = await GetDoctorAsync(doctorId);

            var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "doctors", doctorId.ToString());
            Directory.CreateDirectory(uploadPath);

            doctor.MedicalLicensePath = await SaveFile(dto.MedicalLicense, uploadPath, "license");
            doctor.MdcnCertificatePath = await SaveFile(dto.MdcnCertificate, uploadPath, "mdcn");
            doctor.CvPath = await SaveFile(dto.Cv, uploadPath, "cv");
            doctor.NinSlipPath = await SaveFile(dto.NinSlip, uploadPath, "nin");
            doctor.PassportPath = await SaveFile(dto.Passport, uploadPath, "passport");
            doctor.AdditionalCertificatePath = await SaveFile(dto.AdditionalCertificate, uploadPath, "extra");

            doctor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveComplianceAsync(Guid doctorId, ComplianceRequestDto dto)
        {
            var doctor = await GetDoctorAsync(doctorId);

            doctor.AcceptTerms = dto.AcceptTerms;
            doctor.AcceptPrivacy = dto.AcceptPrivacy;
            doctor.AcceptDataUse = dto.AcceptDataUse;
            doctor.AcceptTelemedicine = dto.AcceptTelemedicine;

            doctor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SaveScheduleAsync(Guid doctorId, ScheduleRequestDto dto)
        {
            var doctor = await GetDoctorAsync(doctorId);

            doctor.PreferredInterview = dto.PreferredDate.Date + dto.PreferredTime;
            doctor.InterviewNotes = dto.Notes;

            doctor.AlternativeSlots.Clear();
            foreach (var alt in dto.Alternatives)
            {
                doctor.AlternativeSlots.Add(new AlternativeInterviewSlot { DateTime = alt.Date.Date + alt.Time });
            }

            doctor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<DoctorCompleteResponseDto> CompleteRegistrationAsync(Guid doctorId)
        {
            var doctor = await GetDoctorAsync(doctorId);

            return new DoctorCompleteResponseDto(
                doctor.Id,
                doctor.PhoneNumber,
                "Registration completed. Awaiting admin approval."
            );
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

        private static string NormalizePhoneNumber(string phone)
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

            return $"/uploads/doctors/{new DirectoryInfo(folderPath).Name}/{fileName}";
        }
    }
}