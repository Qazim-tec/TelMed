// Services/Interfaces/IRateLimiterService.cs
namespace TelmMed.Api.Services.Interfaces
{
    public interface IRateLimiterService
    {
        Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds);
        Task RecordFailureAsync(string key);
        Task ResetAsync(string key);
    }
}