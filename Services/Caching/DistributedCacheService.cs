using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Services
{
    /// <summary>
    /// Cache-aside on top of IDistributedCache (Redis in the API).
    ///
    /// The cache is an optimization, never a dependency: if Redis fails, the
    /// error is logged and the data comes from the database as if there were
    /// no cache. After a failure the cache is skipped for a short break
    /// (a simple circuit breaker), so a Redis outage doesn't make every
    /// request wait for a Redis timeout first.
    /// </summary>
    public class DistributedCacheService : ICacheService
    {
        public static readonly TimeSpan DefaultBreakDuration = TimeSpan.FromSeconds(30);

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;
        private readonly TimeProvider _time;
        private readonly TimeSpan _breakDuration;

        // Registered as a singleton, so the break is shared by all requests.
        private long _skipCacheUntilTicks;

        public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger,
            TimeProvider? time = null, TimeSpan? breakDuration = null)
        {
            _cache = cache;
            _logger = logger;
            _time = time ?? TimeProvider.System;
            _breakDuration = breakDuration ?? DefaultBreakDuration;
        }

        private bool CacheOnBreak => _time.GetUtcNow().UtcTicks < Interlocked.Read(ref _skipCacheUntilTicks);

        private void OnCacheFailure(Exception ex, string operation, string key)
        {
            Interlocked.Exchange(ref _skipCacheUntilTicks, (_time.GetUtcNow() + _breakDuration).UtcTicks);
            _logger.LogWarning(ex, "Cache {Operation} failed for {Key}; using the database only for the next {Seconds}s",
                operation, key, _breakDuration.TotalSeconds);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> load, TimeSpan ttl)
        {
            if (CacheOnBreak)
                return await load();

            try
            {
                string? cached = await _cache.GetStringAsync(key);
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit: {Key}", key);
                    return JsonSerializer.Deserialize<T>(cached, JsonOptions)!;
                }
                _logger.LogInformation("Cache miss: {Key}, loading from the database", key);
            }
            catch (Exception ex)
            {
                OnCacheFailure(ex, "read", key);
                return await load();
            }

            T value = await load();

            try
            {
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value, JsonOptions),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            }
            catch (Exception ex)
            {
                OnCacheFailure(ex, "write", key);
            }

            return value;
        }

        public async Task<string?> GetStringAsync(string key)
        {
            if (CacheOnBreak)
                return null;
            try
            {
                return await _cache.GetStringAsync(key);
            }
            catch (Exception ex)
            {
                OnCacheFailure(ex, "read", key);
                return null;
            }
        }

        public async Task SetStringAsync(string key, string value)
        {
            if (CacheOnBreak)
                return;
            try
            {
                await _cache.SetStringAsync(key, value);
            }
            catch (Exception ex)
            {
                OnCacheFailure(ex, "write", key);
            }
        }
    }
}
