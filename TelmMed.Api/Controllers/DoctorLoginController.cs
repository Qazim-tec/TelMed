using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Data;
using TelmMed.Api.DTOs.Doctors;
using TelmMed.Api.Models;
using TelmMed.Api.Services.DoctorService;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/doctor/login")]
    [ApiController]
    public class DoctorLoginController : ControllerBase
    {
        private readonly IDoctorLoginService _loginService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly AppDbContext _context;

        public DoctorLoginController(
            IDoctorLoginService loginService,
            IHttpContextAccessor httpContext,
            AppDbContext context)
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
            var normalized = DoctorLoginService.NormalizePhone(dto.PhoneNumber);
            var exists = await _loginService.ValidatePhoneAsync(normalized);

            if (!exists)
                return NotFound(new { error = "Phone not registered, not verified, or not approved." });

            // Store in session
            _httpContext.HttpContext.Items["PendingDoctorPhone"] = normalized;

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
            var phone = _httpContext.HttpContext.Items["PendingDoctorPhone"]?.ToString()
                        ?? throw new UnauthorizedAccessException("Session expired. Start again.");

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.PhoneNumber == phone && d.Status == DoctorStatus.Approved)
                ?? throw new UnauthorizedAccessException("Invalid session.");

            var result = await _loginService.VerifyPinAsync(doctor.Id, dto.Pin, dto.UseBiometric);

            // Clear session
            _httpContext.HttpContext.Items.Remove("PendingDoctorPhone");

            return Ok(result);
        }

        private async Task<bool> IsBiometricAvailable(string phone)
        {
            return await _context.Doctors
                .AnyAsync(d => d.PhoneNumber == phone && d.BiometricEnabled);
        }
    }
}
