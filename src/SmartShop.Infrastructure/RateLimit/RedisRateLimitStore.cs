using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SmartShop.Infrastructure.RateLimit;

public class RedisRateLimitStore(IConnectionMultiplexer multiplexer, ILogger<RedisRateLimitStore> logger)
    : IRateLimitStore
{
    // Atomic Lua script:
    // KEYS[1] = key, ARGV[1] = window in seconds
    // Returns {count, ttl}
    private const string IncrementScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        local ttl = redis.call('TTL', KEYS[1])
        return {current, ttl}
        """;

    public async Task<(long Count, DateTimeOffset ResetAt)> IncrementAsync(
        string key, TimeSpan window, CancellationToken ct = default)
    {
        try
        {
            var db = multiplexer.GetDatabase();
            var redisKey = $"rl:{key}";

            var rawResult = (RedisResult[]?)await db.ScriptEvaluateAsync(
                IncrementScript,
                keys: [redisKey],
                values: [(long)window.TotalSeconds]);

            if (rawResult is null)
                return (1, DateTimeOffset.UtcNow.Add(window));

            var count = (long)rawResult[0];
            var ttlSeconds = (long)rawResult[1];

            // TTL -1 means no expire was set (race condition edge case — use window as fallback)
            var resetAt = ttlSeconds > 0
                ? DateTimeOffset.UtcNow.AddSeconds(ttlSeconds)
                : DateTimeOffset.UtcNow.Add(window);

            return (count, resetAt);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis rate limit store error for key '{Key}'. Failing open.", key);
            // Fail-open: return (1, now+window) to avoid blocking requests when Redis is down
            return (1, DateTimeOffset.UtcNow.Add(window));
        }
    }
}
