using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Infrastructure.Options;

namespace OneAccess.Infrastructure.Services;

/// <summary>
/// Distributed caching service backed by Redis / IDistributedCache.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly RedisOptions _redisOptions;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IOptions<RedisOptions> redisOptions,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redisOptions = redisOptions.Value;
        _logger = logger;
    }

    private string FormatKey(string key) => $"{_redisOptions.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _distributedCache.GetStringAsync(FormatKey(key), ct);
            if (string.IsNullOrEmpty(data)) return default;

            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache lookup failed for key {Key}", key);
            throw; // Fail closed per OneAccess.md Section 10
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(5)
            };

            await _distributedCache.SetStringAsync(FormatKey(key), json, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache write failed for key {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(FormatKey(key), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache remove failed for key {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken ct = default)
    {
        // For simple keys, removal is best effort or direct key removal
        await RemoveAsync(prefixKey, ct);
    }
}
