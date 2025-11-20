using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt; 
using TelmMed.Api.DTOs;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/registration")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _service;

        public RegistrationController(IRegistrationService service)
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
                    ExpiresIn = 86400
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("categories")]
        public async Task<IActionResult> SaveCategories([FromBody] SelectCategoriesRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SaveCategoriesAsync(patientId, dto.Categories);
            return Ok(new { success = true });
        }

        [Authorize]
        [HttpPost("language")]
        public async Task<IActionResult> SaveLanguage([FromBody] LanguagePreferencesRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SaveLanguagePreferencesAsync(patientId, dto);
            return Ok(new { success = true });
        }

        [Authorize]
        [HttpPost("personal-info")]
        public async Task<IActionResult> SavePersonalInfo([FromBody] PersonalInfoRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SavePersonalInfoAsync(patientId, dto);
            return Ok(new { success = true });
        }

        [Authorize]
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] SecuritySetupRequestDto dto)
        {
            var patientId = GetPatientId();
            var result = await _service.CompleteRegistrationAsync(patientId, dto);
            return Ok(result);
        }

        // FIXED: Now works with JwtRegisteredClaimNames
        private Guid GetPatientId()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? throw new UnauthorizedAccessException("Invalid token.");

            return Guid.Parse(sub);
        }
    }
}