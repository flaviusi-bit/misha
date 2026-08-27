using System.Collections.Concurrent;
using Misha.Application.Caching;

namespace Misha.Infrastructure.Caching;

/// <summary>
/// Local fallback implementation of the Redis abstraction.
/// This is intentionally non-persistent and is not a substitute for a shared
/// Redis deployment in a multi-instance environment.
/// </summary>
public sealed class InMemoryRedisCache : IRedisCache
{
    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!entries.TryGetValue(key, out var entry))
            return Task.FromResult<string?>(null);

        if (entry.ExpiresAtUtc is { } expiry && expiry <= DateTimeOffset.UtcNow)
        {
            entries.TryRemove(key, out _);
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(entry.Value);
    }

    public Task SetAsync(
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        var expiresAtUtc = expiration is { } ttl
            ? DateTimeOffset.UtcNow.Add(ttl)
            : null;

        entries[key] = new Entry(value, expiresAtUtc);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private sealed record Entry(string Value, DateTimeOffset? ExpiresAtUtc);
}
