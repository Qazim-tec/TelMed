// Services/RateLimiter/RedisRateLimiterService.cs
using StackExchange.Redis;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Services.RateLimiter
{
    public class RedisRateLimiterService : IRateLimiterService
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _db;

        public RedisRateLimiterService(IConnectionMultiplexer multiplexer)
        {
            _multiplexer = multiplexer;
            _db = multiplexer.GetDatabase();
        }

        public async Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds)
        {
            // If Redis is down → allow all requests (fail-open = never block real users)
            if (!_multiplexer.IsConnected)
                return true;

            try
            {
                var countKey = $"rate:{key}";
                var luaScript = @"
                    local current = redis.call('INCR', KEYS[1])
                    if current == 1 then
                        redis.call('EXPIRE', KEYS[1], ARGV[1])
                    end
                    if current > tonumber(ARGV[2]) then
                        return 0
                    end
                    return 1";

                var result = await _db.ScriptEvaluateAsync(
                    luaScript,
                    new RedisKey[] { countKey },
                    new RedisValue[] { windowSeconds, maxAttempts });

                return (int)result == 1;
            }
            catch (RedisConnectionException)
            {
                return true; // Redis down → allow
            }
            catch (RedisTimeoutException)
            {
                return true; // Timeout → allow
            }
            catch
            {
                return true; // Any error → allow (never 500)
            }
        }

        public async Task RecordFailureAsync(string key)
        {
            if (!_multiplexer.IsConnected) return;

            try
            {
                var countKey = $"rate:{key}";
                await _db.StringIncrementAsync(countKey);
            }
            catch { /* silently ignore */ }
        }

        public async Task ResetAsync(string key)
        {
            if (!_multiplexer.IsConnected) return;

            try
            {
                var countKey = $"rate:{key}";
                await _db.KeyDeleteAsync(countKey);
            }
            catch { /* ignore */ }
        }

        public bool IsConnected => _multiplexer.IsConnected;
    }
}