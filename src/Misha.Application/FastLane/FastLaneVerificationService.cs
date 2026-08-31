using System.Security.Cryptography;
using System.Text;
using Misha.Application.Etas;

namespace Misha.Application.FastLane;

public sealed class FastLaneVerificationService(
    IFastLaneVerificationCache cache,
    IEtaCredentialSigner signer)
{
    public bool Verify(FastLanePackage package, DateTimeOffset now)
    {
        var cacheKey = BuildCacheKey(package);
        if (cache.TryGet(cacheKey, now, out var cached))
            return cached;

        var valid = FastLaneVerifier.Verify(package, now, signer.KeyId, signer.PublicKeyPem);
        if (package.ExpiresAtUtc > now)
            cache.Set(cacheKey, valid, package.ExpiresAtUtc);

        return valid;
    }

    private static string BuildCacheKey(FastLanePackage package)
    {
        var canonical = string.Join("|", package.Version, package.EtaNumber,
            package.IssuedAtUtc.ToUnixTimeSeconds(), package.ExpiresAtUtc.ToUnixTimeSeconds(),
            package.SigningKeyId, package.SigningAlgorithm, package.Signature, package.PublicKeyPem);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
