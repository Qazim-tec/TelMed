// Controllers/PatientLoginController.cs
using Microsoft.AspNetCore.Mvc;
using TelmMed.Api.DTOs;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Controllers
{
    [Route("api/patient")]
    [ApiController]
    public class PatientLoginController : ControllerBase
    {
        private readonly IPatientLoginService _loginService;

        public PatientLoginController(IPatientLoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] PatientLoginRequestDto dto)
        {
            try
            {
                var token = await _loginService.LoginWithPinAsync(dto.PhoneNumber, dto.Pin);
                return Ok(new { success = true, token, message = "Login successful!" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("forgot-pin")]
        public async Task<IActionResult> ForgotPin([FromBody] ForgotPinRequestDto dto)
        {
            try
            {
                await _loginService.RequestPinResetAsync(dto.PhoneNumber);
                return Ok(new { success = true, message = "OTP sent to your phone." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("reset-pin")]
        public async Task<IActionResult> ResetPin([FromBody] ResetPinWithOtpDto dto)
        {
            try
            {
                var token = await _loginService.ResetPinWithOtpAsync(dto.PhoneNumber, dto.FirebaseIdToken, dto.NewPin);
                return Ok(new { success = true, token, message = "PIN reset successful! You are logged in." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}