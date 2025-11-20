using TelmMed.Api.DTOs.Doctors;

namespace TelmMed.Api.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<DoctorListItemDto>> GetPendingDoctorsAsync();
        Task<DoctorDetailDto> GetDoctorForReviewAsync(Guid doctorId);
        Task<bool> ReviewDoctorAsync(Guid doctorId, ReviewActionDto dto, Guid adminId);
    }
}
