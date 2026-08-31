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
        var expiry = package.ExpiresAtUtc;
        if (expiry > now)
            cache.Set(cacheKey, valid, expiry);

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

public static class FastLaneVerifier
{
    public static bool Verify(
        FastLanePackage package,
        DateTimeOffset now,
        string trustedKeyId,
        string? trustedPublicKeyPem)
    {
        if (!string.Equals(package.Version, "misha-fastlane-v1", StringComparison.Ordinal) ||
            !string.Equals(package.SigningAlgorithm, "ES256", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(package.Signature) ||
            string.IsNullOrWhiteSpace(package.PublicKeyPem) ||
            string.IsNullOrWhiteSpace(package.EtaNumber) ||
            string.IsNullOrWhiteSpace(trustedKeyId) ||
            string.IsNullOrWhiteSpace(trustedPublicKeyPem) ||
            !string.Equals(package.SigningKeyId, trustedKeyId, StringComparison.Ordinal) ||
            now >= package.ExpiresAtUtc ||
            now < package.IssuedAtUtc)
            return false;

        try
        {
            using var trustedKey = ECDsa.Create();
            trustedKey.ImportFromPem(trustedPublicKeyPem);

            using var packageKey = ECDsa.Create();
            packageKey.ImportFromPem(package.PublicKeyPem);

            if (!CryptographicOperations.FixedTimeEquals(
                    trustedKey.ExportSubjectPublicKeyInfo(),
                    packageKey.ExportSubjectPublicKeyInfo()))
                return false;

            var payload = EtaCredentialPayload.Canonicalize(
                package.EtaNumber,
                package.IssuedAtUtc,
                package.ExpiresAtUtc);

            var signature = FromBase64Url(package.Signature);
            return trustedKey.VerifyData(
                Encoding.UTF8.GetBytes(payload),
                signature,
                HashAlgorithmName.SHA256);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
