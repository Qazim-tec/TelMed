using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public DoctorRegistrationController(IDoctorRegistrationService service) => _service = service;

        [HttpPost("verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequestDto dto)
        {
            var res = await _service.VerifyPhoneAsync(dto.FirebaseIdToken);
            return Ok(new { res.DoctorId, res.PhoneNumber, Token = res.JwtToken });
        }

        [HttpPost("identity")]
        [Authorize]
        public async Task<IActionResult> SaveIdentity([FromBody] IdentityRequestDto dto)
        {
            var id = GetDoctorId();
            await _service.SaveIdentityAsync(id, dto);
            return Ok(new { message = "Identity saved" });
        }

        [HttpPost("practice")]
        [Authorize]
        public async Task<IActionResult> SavePractice([FromBody] PracticeProfileRequestDto dto)
        {
            var id = GetDoctorId();
            await _service.SavePracticeAsync(id, dto);
            return Ok(new { message = "Practice info saved" });
        }

        [HttpPost("credentials")]
        [Authorize]
        public async Task<IActionResult> SaveCredentials([FromBody] CredentialsRequestDto dto)  // ← FIXED: Added [FromBody]
        {
            var id = GetDoctorId();
            await _service.SaveCredentialsAsync(id, dto);
            return Ok(new { message = "Credentials uploaded" });
        }

        [HttpPost("compliance")]
        [Authorize]
        public async Task<IActionResult> SaveCompliance([FromBody] ComplianceRequestDto dto)
        {
            var id = GetDoctorId();
            await _service.SaveComplianceAsync(id, dto);
            return Ok(new { message = "Compliance documents saved" });
        }

        [HttpPost("schedule")]
        [Authorize]
        public async Task<IActionResult> SaveSchedule([FromBody] ScheduleRequestDto dto)
        {
            var id = GetDoctorId();
            await _service.SaveScheduleAsync(id, dto);
            return Ok(new { message = "Schedule saved" });
        }

        [HttpPost("complete")]
        [Authorize]
        public async Task<IActionResult> CompleteRegistration()
        {
            var id = GetDoctorId();
            var res = await _service.CompleteRegistrationAsync(id);
            return Ok(res);
        }

        private Guid GetDoctorId() =>
            Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new UnauthorizedAccessException("Invalid token"));
    }
}