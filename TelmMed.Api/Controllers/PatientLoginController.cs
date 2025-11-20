using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using TelmMed.Api.DTOs;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/patient")]  // ← CHANGED FROM "api/registration" TO "api/patient"
    [ApiController]
    public class PatientRegistrationController : ControllerBase  // ← Renamed for clarity
    {
        private readonly IRegistrationService _service;
        public PatientRegistrationController(IRegistrationService service)
        {
            _service = service;
        }

        [HttpPost("verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequestDto dto)
        {
            try
            {
                var result = await _service.VerifyPhoneWithFirebaseAsync(dto.FirebaseIdToken);
                return Ok(new
                {
                    result.PatientId,
                    result.PhoneNumber,
                    Token = result.JwtToken,
                    ExpiresIn = 86400, // 24 hours
                    message = "Phone verified. Continue registration."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Verification failed", details = ex.Message });
            }
        }

        [Authorize(Policy = "Patient")]  // Extra safety
        [HttpPost("categories")]
        public async Task<IActionResult> SaveCategories([FromBody] SelectCategoriesRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SaveCategoriesAsync(patientId, dto.Categories);
            return Ok(new { success = true, message = "Categories saved" });
        }

        [Authorize(Policy = "Patient")]
        [HttpPost("language")]
        public async Task<IActionResult> SaveLanguage([FromBody] LanguagePreferencesRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SaveLanguagePreferencesAsync(patientId, dto);
            return Ok(new { success = true });
        }

        [Authorize(Policy = "Patient")]
        [HttpPost("personal-info")]
        public async Task<IActionResult> SavePersonalInfo([FromBody] PersonalInfoRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SavePersonalInfoAsync(patientId, dto);
            return Ok(new { success = true });
        }

        [Authorize(Policy = "Patient")]
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] SecuritySetupRequestDto dto)
        {
            var patientId = GetPatientId();
            var result = await _service.CompleteRegistrationAsync(patientId, dto);
            return Ok(result);
        }

        private Guid GetPatientId()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? throw new UnauthorizedAccessException("Invalid token.");
            return Guid.Parse(sub);
        }
    }
}