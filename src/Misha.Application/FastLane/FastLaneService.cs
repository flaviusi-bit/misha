using System.Security.Cryptography;
using System.Text;
using Misha.Application.Applications;
using Misha.Application.Etas;
using Misha.Application.Payments;
using Misha.Domain.Applications;
using Misha.Domain.Etas;
using Misha.Domain.Payments;

namespace Misha.Application.FastLane;

public sealed class FastLaneService(
    IApplicationRepository applications,
    IPaymentRepository payments,
    IEtaRepository etas,
    IEtaCredentialSigner signer)
{
    public async Task<FastLanePackage> CreatePackageAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await applications.GetAsync(applicationId, cancellationToken) ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");
        if (application.Status != ApplicationStatus.Approved) throw new InvalidOperationException("Fast Lane is available only for approved applications.");
        var payment = await payments.GetLatestAsync(applicationId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Paid) throw new InvalidOperationException("Fast Lane requires a paid application.");
        var eta = await etas.GetByApplicationIdAsync(applicationId, cancellationToken) ?? throw new InvalidOperationException("Fast Lane requires an issued eTA.");
        if (!eta.IsValidAt(DateTimeOffset.UtcNow)) throw new InvalidOperationException("Fast Lane requires a currently valid eTA.");
        if (!signer.IsEnabled || string.IsNullOrWhiteSpace(signer.PublicKeyPem)) throw new InvalidOperationException("Fast Lane requires cryptographic eTA signing to be enabled.");
        var signature = signer.Sign(eta.EtaNumber, eta.IssuedAtUtc, eta.ExpiresAtUtc);
        if (string.IsNullOrWhiteSpace(signature)) throw new InvalidOperationException("Fast Lane credential signing failed.");
        return new FastLanePackage("misha-fastlane-v1", eta.EtaNumber, eta.IssuedAtUtc, eta.ExpiresAtUtc, signer.KeyId, signer.Algorithm, signature, signer.PublicKeyPem);
    }
}

public static class FastLaneVerifier
{
    public static bool Verify(FastLanePackage package, DateTimeOffset now) => Verify(package, now, null, null);

    public static bool Verify(FastLanePackage package, DateTimeOffset now, string? trustedKeyId, string? trustedPublicKeyPem)
    {
        if (!string.Equals(package.Version, "misha-fastlane-v1", StringComparison.Ordinal) ||
            !string.Equals(package.SigningAlgorithm, "ES256", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(package.Signature) || string.IsNullOrWhiteSpace(package.PublicKeyPem) ||
            string.IsNullOrWhiteSpace(package.EtaNumber) || now >= package.ExpiresAtUtc || now < package.IssuedAtUtc)
            return false;

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(package.PublicKeyPem);

            if (trustedKeyId is not null || trustedPublicKeyPem is not null)
            {
                if (string.IsNullOrWhiteSpace(trustedKeyId) || string.IsNullOrWhiteSpace(trustedPublicKeyPem) ||
                    !string.Equals(package.SigningKeyId, trustedKeyId, StringComparison.Ordinal))
                    return false;
                using var trustedKey = ECDsa.Create();
                trustedKey.ImportFromPem(trustedPublicKeyPem);
                if (!CryptographicOperations.FixedTimeEquals(ecdsa.ExportSubjectPublicKeyInfo(), trustedKey.ExportSubjectPublicKeyInfo()))
                    return false;
                ecdsa.ImportFromPem(trustedPublicKeyPem);
            }

            var payload = EtaCredentialPayload.Canonicalize(package.EtaNumber, package.IssuedAtUtc, package.ExpiresAtUtc);
            return ecdsa.VerifyData(Encoding.UTF8.GetBytes(payload), FromBase64Url(package.Signature), HashAlgorithmName.SHA256);
        }
        catch (CryptographicException) { return false; }
        catch (FormatException) { return false; }
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
