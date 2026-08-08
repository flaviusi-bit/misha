using System.Security.Cryptography;
using System.Text;
using Misha.Application.Etas;

namespace Misha.Infrastructure;

public sealed class EcdsaEtaCredentialSigner : IEtaCredentialSigner, IDisposable
{
    private readonly ECDsa _ecdsa;

    public EcdsaEtaCredentialSigner(string keyId, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Signing key id is required.", nameof(keyId));

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("Signing private key is required.", nameof(privateKeyPem));

        KeyId = keyId.Trim();
        _ecdsa = ECDsa.Create();
        _ecdsa.ImportFromPem(privateKeyPem);
    }

    public bool IsEnabled => true;
    public string KeyId { get; }
    public string Algorithm => "ES256";
    public string PublicKeyPem => _ecdsa.ExportSubjectPublicKeyInfoPem();

    public string? Sign(string etaNumber, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc)
    {
        var payload = EtaCredentialPayload.Canonicalize(etaNumber, issuedAtUtc, expiresAtUtc);
        var signature = _ecdsa.SignData(
            Encoding.UTF8.GetBytes(payload),
            HashAlgorithmName.SHA256);

        return Convert.ToBase64UrlString(signature);
    }

    public void Dispose() => _ecdsa.Dispose();
}

public sealed class DisabledEtaCredentialSigner : IEtaCredentialSigner
{
    public bool IsEnabled => false;
    public string KeyId => string.Empty;
    public string Algorithm => "ES256";
    public string? PublicKeyPem => null;

    public string? Sign(string etaNumber, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc) => null;
}

internal static class Base64UrlExtensions
{
    public static string ToBase64UrlString(this byte[] value) =>
        Convert.ToBase64String(value)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
}
