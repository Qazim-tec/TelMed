namespace TelmMed.Api.Services.Interfaces
{
    public interface IJwtService
    {
       
            string GenerateToken(Guid userId, string phoneNumber, string role);
            bool ValidateToken(string token, out Guid userId);
            bool ValidateToken(string token, out Guid userId, out string? phoneNumber, out string? role);
       
    }
  
}
