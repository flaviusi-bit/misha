using System.Security.Cryptography;
using System.Text;
using Misha.Application.Etas;
using Misha.Application.FastLane;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class FastLaneVerificationCacheTests
{
    [Fact]
    public void Verify_uses_cached_result_until_package_expiry()
    {
        var cache = new RecordingCache();
        var service = new FastLaneVerificationService(cache);
        var package = CreateValidPackage();
        var now = package.IssuedAtUtc.AddMinutes(1);

        Assert.True(service.Verify(package, now));
        Assert.True(service.Verify(package, now.AddMinutes(1)));
        Assert.Equal(1, cache.GetCount);
        Assert.Equal(1, cache.SetCount);
        Assert.Equal(package.ExpiresAtUtc, cache.ExpiresAtUtc);
    }

    [Fact]
    public void Verify_does_not_use_cache_after_expiry()
    {
        var cache = new RecordingCache();
        var service = new FastLaneVerificationService(cache);
        var package = CreateValidPackage();
        var now = package.IssuedAtUtc.AddMinutes(1);

        Assert.True(service.Verify(package, now));
        Assert.False(service.Verify(package, package.ExpiresAtUtc));
        Assert.Equal(2, cache.GetCount);
        Assert.Equal(1, cache.SetCount);
    }

    private static FastLanePackage CreateValidPackage()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var etaNumber = "ETA-CACHE-001";
        var issued = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expires = DateTimeOffset.UtcNow.AddDays(1);
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issued, expires);
        var signature = Convert.ToBase64String(ecdsa.SignData(
            Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        return new FastLanePackage("misha-fastlane-v1", etaNumber, issued, expires,
            "key-1", "ES256", signature, ecdsa.ExportSubjectPublicKeyInfoPem());
    }

    private sealed class RecordingCache : IFastLaneVerificationCache
    {
        private readonly Dictionary<string, (bool Value, DateTimeOffset Expiry)> entries = new();
        public int GetCount { get; private set; }
        public int SetCount { get; private set; }
        public DateTimeOffset ExpiresAtUtc { get; private set; }

        public bool TryGet(string cacheKey, DateTimeOffset now, out bool isValid)
        {
            GetCount++;
            if (entries.TryGetValue(cacheKey, out var entry) && now < entry.Expiry)
            {
                isValid = entry.Value;
                return true;
            }
            isValid = false;
            return false;
        }

        public void Set(string cacheKey, bool isValid, DateTimeOffset expiresAtUtc)
        {
            SetCount++;
            ExpiresAtUtc = expiresAtUtc;
            entries[cacheKey] = (isValid, expiresAtUtc);
        }

        public void Remove(string cacheKey) => entries.Remove(cacheKey);
    }
}
