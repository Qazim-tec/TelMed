using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using TelmMed.Api.DTOs.Doctors;
using TelmMed.Api.Models;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _service;

        public AdminController(IAdminService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all pending doctor registrations
        /// </summary>
        [HttpGet("doctors/pending")]
        public async Task<IActionResult> GetPendingDoctors()
        {
            var doctors = await _service.GetPendingDoctorsAsync();
            return Ok(doctors);
        }

        /// <summary>
        /// Get full details of a doctor for review
        /// </summary>
        [HttpGet("doctors/{id}")]
        public async Task<IActionResult> GetDoctor(Guid id)
        {
            try
            {
                var doctor = await _service.GetDoctorForReviewAsync(id);
                return Ok(doctor);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Doctor not found." });
            }
        }

        /// <summary>
        /// Approve or reject a doctor
        /// </summary>
        [HttpPost("doctors/{id}/review")]
        public async Task<IActionResult> ReviewDoctor(Guid id, [FromBody] ReviewActionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            await _service.ReviewDoctorAsync(id, dto, adminId);

            return Ok(new
            {
                success = true,
                message = dto.Status == DoctorStatus.Approved
                    ? "Doctor approved successfully."
                    : "Doctor rejected."
            });
        }

        private Guid GetAdminId()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? throw new UnauthorizedAccessException("Invalid admin token.");
            return Guid.Parse(sub);
        }
    }
}
