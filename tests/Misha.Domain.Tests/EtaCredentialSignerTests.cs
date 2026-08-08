using System.Security.Cryptography;
using System.Text;
using Misha.Application.Etas;
using Misha.Infrastructure;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class EtaCredentialSignerTests
{
    [Fact]
    public void Ecdsa_signer_creates_a_signature_verifiable_with_its_public_key()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privateKeyPem = key.ExportPkcs8PrivateKeyPem();
        using var signer = new EcdsaEtaCredentialSigner("test-key-1", privateKeyPem);

        const string etaNumber = "ETA-ABC123";
        var issuedAt = new DateTimeOffset(2026, 8, 8, 22, 0, 0, TimeSpan.Zero);
        var expiresAt = issuedAt.AddDays(90);
        var signature = signer.Sign(etaNumber, issuedAt, expiresAt);

        Assert.Equal("ES256", signer.Algorithm);
        Assert.Equal("test-key-1", signer.KeyId);
        Assert.NotNull(signer.PublicKeyPem);
        Assert.NotEmpty(signature!);

        using var verifier = ECDsa.Create();
        verifier.ImportFromPem(signer.PublicKeyPem);
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issuedAt, expiresAt);
        var signatureBytes = Convert.FromBase64String(signature!
            .Replace("-", "+", StringComparison.Ordinal)
            .Replace("_", "/", StringComparison.Ordinal) + "==");

        Assert.True(verifier.VerifyData(
            Encoding.UTF8.GetBytes(payload),
            signatureBytes,
            HashAlgorithmName.SHA256));
    }

    [Fact]
    public void Canonical_payload_is_stable_for_equivalent_utc_values()
    {
        var first = new DateTimeOffset(2026, 8, 8, 22, 0, 0, TimeSpan.Zero);
        var second = new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(
            EtaCredentialPayload.Canonicalize(" ETA-ABC123 ", first, first.AddDays(90)),
            EtaCredentialPayload.Canonicalize("ETA-ABC123", second, second.AddDays(90)));
    }
}
