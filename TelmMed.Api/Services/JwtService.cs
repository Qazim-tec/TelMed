// Services/JwtService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Services
{
    public class JwtService : IJwtService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration config)
        {
            var jwt = config.GetSection("Jwt");
            _secret = jwt["Secret"] ?? throw new ArgumentNullException("Jwt:Secret missing");
            _issuer = jwt["Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer missing");
            _audience = jwt["Audience"] ?? throw new ArgumentNullException("Jwt:Audience missing");
            _expiryMinutes = int.Parse(jwt["ExpiryMinutes"] ?? "1440");
        }

        // FIXED: role is REQUIRED
        public string GenerateToken(Guid userId, string phoneNumber, string role)
        {
            if (string.IsNullOrEmpty(role))
                throw new ArgumentNullException(nameof(role), "Role is required for JWT.");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, phoneNumber),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // FIXED: Returns full claims (userId, phone, role)
        public bool ValidateToken(string token, out Guid userId, out string? phoneNumber, out string? role)
        {
            userId = Guid.Empty;
            phoneNumber = null;
            role = null;

            var key = Encoding.UTF8.GetBytes(_secret);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(2)
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                var subClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
                phoneNumber = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value;
                role = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                return subClaim != null && Guid.TryParse(subClaim, out userId);
            }
            catch
            {
                return false;
            }
        }

        // Optional: Simple version (backwards compatible)
        public bool ValidateToken(string token, out Guid userId) =>
            ValidateToken(token, out userId, out _, out _);
    }
}