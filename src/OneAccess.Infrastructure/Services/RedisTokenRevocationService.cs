using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Infrastructure.Options;

namespace OneAccess.Infrastructure.Services;

/// <summary>
/// Service tracking and checking user token revocation timestamps in Redis.
/// </summary>
public class RedisTokenRevocationService : ITokenRevocationService
{
    private readonly IDistributedCache _distributedCache;
    private readonly RedisOptions _redisOptions;
    private readonly ILogger<RedisTokenRevocationService> _logger;

    public RedisTokenRevocationService(
        IDistributedCache distributedCache,
        IOptions<RedisOptions> redisOptions,
        ILogger<RedisTokenRevocationService> logger)
    {
        _distributedCache = distributedCache;
        _redisOptions = redisOptions.Value;
        _logger = logger;
    }

    private string GetKey(Guid userId) => $"{_redisOptions.InstanceName}revoked-after:{userId}";

    public async Task RevokeAsync(Guid userId, DateTimeOffset revokedAfter, CancellationToken ct = default)
    {
        try
        {
            var unixSeconds = revokedAfter.ToUnixTimeSeconds().ToString();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };
            await _distributedCache.SetStringAsync(GetKey(userId), unixSeconds, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write token revocation for user {UserId}", userId);
            throw; // Fail-closed per Section 10
        }
    }

    public async Task<bool> IsRevokedAsync(Guid userId, long tokenIssuedAtUnixSeconds, CancellationToken ct = default)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(GetKey(userId), ct);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (long.TryParse(value, out var revokedAfterUnixSeconds))
            {
                return tokenIssuedAtUnixSeconds < revokedAfterUnixSeconds;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check token revocation for user {UserId}", userId);
            throw; // Fail-closed per Section 10
        }
    }
}
