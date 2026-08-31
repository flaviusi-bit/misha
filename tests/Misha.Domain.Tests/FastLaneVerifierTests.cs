using System.Security.Cryptography;
using System.Text;
using Misha.Application.Etas;
using Misha.Application.FastLane;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class FastLaneVerifierTests
{
    [Fact]
    public void Verify_accepts_valid_signed_package_with_trusted_key()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var etaNumber = "ETA-ABC123";
        var issued = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expires = DateTimeOffset.UtcNow.AddDays(10);
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issued, expires);
        var signature = ToBase64Url(ecdsa.SignData(
            Encoding.UTF8.GetBytes(payload),
            HashAlgorithmName.SHA256));
        var publicKey = ecdsa.ExportSubjectPublicKeyInfoPem();
        var package = new FastLanePackage(
            "misha-fastlane-v1", etaNumber, issued, expires, "key-1", "ES256", signature, publicKey);

        Assert.True(FastLaneVerifier.Verify(package, DateTimeOffset.UtcNow, "key-1", publicKey));
    }

    [Fact]
    public void Verify_rejects_package_signed_by_untrusted_public_key()
    {
        using var trusted = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var attacker = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var etaNumber = "ETA-ATTACK";
        var issued = DateTimeOffset.UtcNow.AddMinutes(-1);
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issued, expires);
        var signature = ToBase64Url(attacker.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256));
        var package = new FastLanePackage(
            "misha-fastlane-v1", etaNumber, issued, expires, "key-1", "ES256", signature,
            attacker.ExportSubjectPublicKeyInfoPem());

        Assert.False(FastLaneVerifier.Verify(
            package, DateTimeOffset.UtcNow, "key-1", trusted.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Verify_rejects_mismatched_signing_key_id()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var etaNumber = "ETA-KEY-MISMATCH";
        var issued = DateTimeOffset.UtcNow.AddMinutes(-1);
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issued, expires);
        var signature = ToBase64Url(ecdsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256));
        var publicKey = ecdsa.ExportSubjectPublicKeyInfoPem();
        var package = new FastLanePackage(
            "misha-fastlane-v1", etaNumber, issued, expires, "key-evil", "ES256", signature, publicKey);

        Assert.False(FastLaneVerifier.Verify(package, DateTimeOffset.UtcNow, "key-1", publicKey));
    }

    [Fact]
    public void Verify_rejects_expired_package()
    {
        var package = new FastLanePackage(
            "misha-fastlane-v1", "ETA-ABC123", DateTimeOffset.UtcNow.AddDays(-20),
            DateTimeOffset.UtcNow.AddDays(-1), "key-1", "ES256", "invalid", "invalid");

        Assert.False(FastLaneVerifier.Verify(package, DateTimeOffset.UtcNow, "key-1", "invalid"));
    }

    private static string ToBase64Url(byte[] value) =>
        Convert.ToBase64String(value)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
}
