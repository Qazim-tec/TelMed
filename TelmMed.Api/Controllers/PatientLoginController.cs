using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data; // ← ADD THIS// ← CORRECT DTO NAMESPACE
using TelmMed.Api.DTOs;
using TelmMed.Api.Services.Interfaces;
using TelmMed.Api.Services.RateLimiter;

namespace TelmMed.Api.Controllers
{
    [Route("api/patient/login")]
    [ApiController]
    public class PatientLoginController : ControllerBase
    {
        private readonly IPatientLoginService _loginService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly AppDbContext _context; // ← ADD THIS

        public PatientLoginController(
            IPatientLoginService loginService,
            IHttpContextAccessor httpContext,
            AppDbContext context) // ← INJECT DB CONTEXT
        {
            _loginService = loginService;
            _httpContext = httpContext;
            _context = context;
        }

        /// <summary>
        /// Step 1: Validate phone number
        /// </summary>
        [HttpPost("phone")]
        public async Task<IActionResult> ValidatePhone([FromBody] PhoneLoginRequestDto dto)
        {
            // ← FIX: Use static method from service
            var normalized = PatientLoginService.NormalizePhone(dto.PhoneNumber);

            var exists = await _loginService.ValidatePhoneAsync(normalized);
            if (!exists)
                return NotFound(new { error = "Phone number not registered or not verified." });

            // Store temporarily in HttpContext
            _httpContext.HttpContext.Items["PendingPatientPhone"] = normalized;

            return Ok(new
            {
                message = "Phone valid. Enter PIN.",
                biometricAvailable = await IsBiometricAvailable(normalized)
            });
        }

        /// <summary>
        /// Step 2: Verify PIN or Biometric
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPin([FromBody] PinLoginRequestDto dto)
        {
            var phone = _httpContext.HttpContext.Items["PendingPatientPhone"]?.ToString()
                        ?? throw new UnauthorizedAccessException("Session expired. Start again.");

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == phone)
                ?? throw new UnauthorizedAccessException("Invalid session.");

            var result = await _loginService.VerifyPinAsync(patient.Id, dto.Pin, dto.UseBiometric);

            // Clear session
            _httpContext.HttpContext.Items.Remove("PendingPatientPhone");

            return Ok(result);
        }

        private async Task<bool> IsBiometricAvailable(string phone)
        {
            return await _context.Patients
                .AnyAsync(p => p.PhoneNumber == phone && p.BiometricEnabled);
        }
    }
}