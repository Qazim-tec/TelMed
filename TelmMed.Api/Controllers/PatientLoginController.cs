using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TelmMed.Api.DTOs;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/patient")]
    [ApiController]
    public class PatientRegistrationController : ControllerBase
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
                    ExpiresIn = 86400,
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

        [Authorize(Policy = "Patient")]
        [HttpPost("categories")]
        public async Task<IActionResult> SaveCategories([FromBody] SelectCategoriesRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SaveCategoriesAsync(patientId, dto.Categories);
            return Ok(new { success = true, message = "Categories saved successfully" });
        }

        [Authorize(Policy = "Patient")]
        [HttpPost("language")]
        public async Task<IActionResult> SaveLanguage([FromBody] LanguagePreferencesRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SaveLanguagePreferencesAsync(patientId, dto);
            return Ok(new { success = true, message = "Language preferences saved" });
        }

        [Authorize(Policy = "Patient")]
        [HttpPost("personal-info")]
        public async Task<IActionResult> SavePersonalInfo([FromBody] PersonalInfoRequestDto dto)
        {
            var patientId = GetPatientId();
            await _service.SavePersonalInfoAsync(patientId, dto);
            return Ok(new { success = true, message = "Personal info saved" });
        }

        [Authorize(Policy = "Patient")]
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] SecuritySetupRequestDto dto)
        {
            var patientId = GetPatientId();
            var result = await _service.CompleteRegistrationAsync(patientId, dto);
            return Ok(new
            {
                success = true,
                message = "Registration completed successfully!",
                patientId = result.PatientId,
                phoneNumber = result.PhoneNumber
            });
        }

        /// <summary>
        /// BULLETPROOF Patient ID extractor – works 100% with .NET JwtBearer middleware quirks
        /// </summary>
        private Guid GetPatientId()
        {
            // JwtBearer sometimes maps "sub" → ClaimTypes.NameIdentifier, sometimes keeps "sub"
            var claimValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                throw new UnauthorizedAccessException("Invalid or missing token: 'sub' claim not found.");

            if (!Guid.TryParse(claimValue, out var patientId))
                throw new UnauthorizedAccessException("Invalid patient ID format in token.");

            return patientId;
        }
    }
}