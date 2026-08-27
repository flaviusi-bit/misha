using System.Collections.Concurrent;
using Misha.Application.FastLane;

namespace Misha.Infrastructure.FastLane;

public sealed class InMemoryFastLaneVerificationCache : IFastLaneVerificationCache
{
    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    public bool TryGet(string cacheKey, DateTimeOffset now, out bool isValid)
    {
        if (entries.TryGetValue(cacheKey, out var entry) && now < entry.ExpiresAtUtc)
        {
            isValid = entry.IsValid;
            return true;
        }

        entries.TryRemove(cacheKey, out _);
        isValid = false;
        return false;
    }

    public void Set(string cacheKey, bool isValid, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));

        entries[cacheKey] = new Entry(isValid, expiresAtUtc);
    }

    public void Remove(string cacheKey) => entries.TryRemove(cacheKey, out _);

    private sealed record Entry(bool IsValid, DateTimeOffset ExpiresAtUtc);
}
