// Services/RateLimiter/RedisRateLimiterService.cs
using StackExchange.Redis;
using TelmMed.Api.Services.Interfaces;

namespace TelmMed.Api.Services.RateLimiter
{
    public class RedisRateLimiterService : IRateLimiterService
    {
        private readonly IDatabase _db;
        private readonly ConnectionMultiplexer _redis;

        public RedisRateLimiterService(IConnectionMultiplexer redis)
        {
            _redis = (ConnectionMultiplexer)redis;
            _db = _redis.GetDatabase();
        }

        public async Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds)
        {
            var countKey = $"rate:{key}";
            var luaScript = @"
                local key = KEYS[1]
                local max = tonumber(ARGV[1])
                local window = tonumber(ARGV[2])
                local current = redis.call('INCR', key)
                if current == 1 then
                    redis.call('EXPIRE', key, window)
                else
                    local ttl = redis.call('TTL', key)
                    if ttl == -1 then
                        redis.call('EXPIRE', key, window)
                    end
                end
                return current <= max
            ";

            var result = await _db.ScriptEvaluateAsync(
                luaScript,
                new RedisKey[] { countKey },
                new RedisValue[] { maxAttempts, windowSeconds }
            );

            return (bool)result;
        }

        public async Task RecordFailureAsync(string key)
        {
            var countKey = $"rate:{key}";
            await _db.StringIncrementAsync(countKey);
            // TTL will be set by IsAllowedAsync
        }

        public async Task ResetAsync(string key)
        {
            var countKey = $"rate:{key}";
            await _db.KeyDeleteAsync(countKey);
        }

        // Optional: Health check
        public bool IsConnected => _redis.IsConnected;
    }
}