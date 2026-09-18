namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Distributed cache abstraction backed by Redis for permissions, division scopes, and sub-system access.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefixKey, CancellationToken ct = default);
}
