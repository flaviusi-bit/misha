using System.Security.Cryptography;
using Misha.Application.Etas;
using Misha.Application.FastLane;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class FastLaneVerifierTests
{
    [Fact]
    public void Verify_accepts_valid_signed_package()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var etaNumber = "ETA-ABC123";
        var issued = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expires = DateTimeOffset.UtcNow.AddDays(10);
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issued, expires);
        var signature = Convert.ToBase64String(ecdsa.SignData(
            System.Text.Encoding.UTF8.GetBytes(payload),
            HashAlgorithmName.SHA256))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var package = new FastLanePackage(
            "misha-fastlane-v1",
            etaNumber,
            issued,
            expires,
            "key-1",
            "ES256",
            signature,
            ecdsa.ExportSubjectPublicKeyInfoPem());

        Assert.True(FastLaneVerifier.Verify(package, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Verify_rejects_expired_package()
    {
        var package = new FastLanePackage(
            "misha-fastlane-v1",
            "ETA-ABC123",
            DateTimeOffset.UtcNow.AddDays(-20),
            DateTimeOffset.UtcNow.AddDays(-1),
            "key-1",
            "ES256",
            "invalid",
            "invalid");

        Assert.False(FastLaneVerifier.Verify(package, DateTimeOffset.UtcNow));
    }
}
