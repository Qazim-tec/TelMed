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
        public async Task<IActionResult> Verify([FromBody] VerifyPhoneRequestDto dto)
        {
            var res = await _service.VerifyPhoneAsync(dto.FirebaseIdToken);
            return Ok(new { res.DoctorId, res.PhoneNumber, Token = res.JwtToken });
        }

        [Authorize][HttpPost("identity")] public async Task<IActionResult> Identity([FromBody] IdentityRequestDto dto) { var id = GetDoctorId(); await _service.SaveIdentityAsync(id, dto); return Ok(); }
        [Authorize][HttpPost("practice")] public async Task<IActionResult> Practice([FromBody] PracticeProfileRequestDto dto) { var id = GetDoctorId(); await _service.SavePracticeAsync(id, dto); return Ok(); }
        [Authorize][HttpPost("credentials")] public async Task<IActionResult> Credentials(CredentialsRequestDto dto) { var id = GetDoctorId(); await _service.SaveCredentialsAsync(id, dto); return Ok(); }
        [Authorize][HttpPost("compliance")] public async Task<IActionResult> Compliance([FromBody] ComplianceRequestDto dto) { var id = GetDoctorId(); await _service.SaveComplianceAsync(id, dto); return Ok(); }
        [Authorize][HttpPost("schedule")] public async Task<IActionResult> Schedule([FromBody] ScheduleRequestDto dto) { var id = GetDoctorId(); await _service.SaveScheduleAsync(id, dto); return Ok(); }
        [Authorize][HttpPost("complete")] public async Task<IActionResult> Complete() { var id = GetDoctorId(); var res = await _service.CompleteRegistrationAsync(id); return Ok(res); }

        private Guid GetDoctorId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? throw new UnauthorizedAccessException());
    }
}
