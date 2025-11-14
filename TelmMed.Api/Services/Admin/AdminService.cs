
    using global::TelmMed.Api.Data;
    using global::TelmMed.Api.DTOs.Doctors;
    using global::TelmMed.Api.Models;
    using global::TelmMed.Api.Services.Interfaces;
    using Microsoft.EntityFrameworkCore;


    namespace TelmMed.Api.Services
    {
        public class AdminService : IAdminService
        {
            private readonly AppDbContext _context;

            public AdminService(AppDbContext context)
            {
                _context = context;
            }

            public async Task<List<DoctorListItemDto>> GetPendingDoctorsAsync()
            {
                return await _context.Doctors
                    .Where(d => d.Status == DoctorStatus.Pending)
                    .Select(d => new DoctorListItemDto(
                        d.Id,
                        d.LegalName,
                        d.PhoneNumber,
                        d.Specialty,
                        d.Status,
                        d.CreatedAt
                    ))
                    .OrderBy(d => d.CreatedAt)
                    .ToListAsync();
            }

            public async Task<DoctorDetailDto> GetDoctorForReviewAsync(Guid doctorId)
            {
                var doctor = await _context.Doctors
                    .Include(d => d.Languages)
                    .Include(d => d.NextOfKin)
                    .FirstOrDefaultAsync(d => d.Id == doctorId)
                    ?? throw new KeyNotFoundException("Doctor not found.");

                return new DoctorDetailDto(
                    doctor.Id,
                    doctor.LegalName,
                    doctor.Email,
                    doctor.PhoneNumber,
                    doctor.Specialty,
                    doctor.CurrentWorkplace,
                    doctor.ShortBio,
                    doctor.Languages.Select(l => $"{l.Name} ({l.Proficiency})").ToList(),
                    doctor.MedicalLicensePath!,
                    doctor.MdcnCertificatePath!,
                    doctor.Status,
                    doctor.RejectionReason,
                    doctor.ReviewedAt
                );
            }

            public async Task<bool> ReviewDoctorAsync(Guid doctorId, ReviewActionDto dto, Guid adminId)
            {
                var doctor = await _context.Doctors.FindAsync(doctorId)
                    ?? throw new KeyNotFoundException("Doctor not found.");

                doctor.Status = dto.Status;
                doctor.RejectionReason = dto.RejectionReason;
                doctor.ReviewedAt = DateTime.UtcNow;
                doctor.ReviewedBy = adminId;

                await _context.SaveChangesAsync();
                return true;
            }
        }
    }
