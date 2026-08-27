namespace Misha.Application.FastLane;

public interface IFastLaneVerificationCache
{
    bool TryGet(string cacheKey, DateTimeOffset now, out bool isValid);

    void Set(string cacheKey, bool isValid, DateTimeOffset expiresAtUtc);

    void Remove(string cacheKey);
}
