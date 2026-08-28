namespace Misha.Application.Caching;

/// <summary>
/// Provider-neutral contract for ephemeral distributed key/value caching.
/// Implementations must treat cache contents as non-authoritative and may evict
/// entries at any time.
/// </summary>
public interface IRedisCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
