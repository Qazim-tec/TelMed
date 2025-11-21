using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using TelmMed.Api.DTOs.Doctors;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/doctor")]
    [ApiController]
    public class DoctorRegistrationController : ControllerBase
    {
        private readonly IDoctorRegistrationService _service;

        public DoctorRegistrationController(IDoctorRegistrationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Step 1: Verify phone number via Firebase OTP
        /// </summary>
        [HttpPost("verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FirebaseIdToken))
                return BadRequest(new { error = "Firebase token is required" });

            var result = await _service.VerifyPhoneAsync(dto.FirebaseIdToken);

            return Ok(new
            {
                doctorId = result.DoctorId,
                phoneNumber = result.PhoneNumber,
                token = result.JwtToken,
                message = "Phone verified successfully. Proceed with registration."
            });
        }

        [HttpPost("identity")]
        [Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SaveIdentity([FromBody] IdentityRequestDto dto)
        {
            var doctorId = GetDoctorId();
            await _service.SaveIdentityAsync(doctorId, dto);
            return Ok(new { success = true, message = "Personal identity saved successfully" });
        }

        [HttpPost("practice")]
        [Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SavePractice([FromBody] PracticeProfileRequestDto dto)
        {
            var doctorId = GetDoctorId();
            await _service.SavePracticeAsync(doctorId, dto);
            return Ok(new { success = true, message = "Practice profile saved successfully" });
        }

        [HttpPost("credentials")]
        [Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SaveCredentials([FromBody] CredentialsRequestDto dto)
        {
            var doctorId = GetDoctorId();
            await _service.SaveCredentialsAsync(doctorId, dto);
            return Ok(new { success = true, message = "Credentials & documents uploaded successfully" });
        }

        [HttpPost("compliance")]
        [Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SaveCompliance([FromBody] ComplianceRequestDto dto)
        {
            var doctorId = GetDoctorId();
            await _service.SaveComplianceAsync(doctorId, dto);
            return Ok(new { success = true, message = "Compliance agreements accepted" });
        }

        [HttpPost("schedule")]
        [Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SaveSchedule([FromBody] ScheduleRequestDto dto)
        {
            var doctorId = GetDoctorId();
            await _service.SaveScheduleAsync(doctorId, dto);
            return Ok(new { success = true, message = "Interview schedule saved" });
        }

        [HttpPost("complete")]
        [Authorize(Policy = "Doctor")]
        public async Task<IActionResult> CompleteRegistration()
        {
            var doctorId = GetDoctorId();
            var result = await _service.CompleteRegistrationAsync(doctorId);
            return Ok(new
            {
                success = true,
                result.DoctorId,
                result.PhoneNumber,
                result.Message
            });
        }

        private Guid GetDoctorId()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? throw new UnauthorizedAccessException("Invalid or missing token");

            if (!Guid.TryParse(sub, out var doctorId))
                throw new UnauthorizedAccessException("Invalid doctor ID in token");

            return doctorId;
        }
    }
}