using System.Security.Cryptography;

namespace Misha.Domain.Etas;

public sealed class Eta
{
    private Eta() { }

    private Eta(
        Guid id,
        Guid applicationId,
        string etaNumber,
        string verificationTokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        ApplicationId = applicationId;
        EtaNumber = etaNumber;
        VerificationTokenHash = verificationTokenHash;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = EtaStatus.Issued;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string EtaNumber { get; private set; } = string.Empty;
    public string VerificationTokenHash { get; private set; } = string.Empty;
    public EtaStatus Status { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    public static (Eta Eta, string VerificationToken) Issue(
        Guid applicationId,
        int validityDays,
        DateTimeOffset? now = null)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));

        if (validityDays <= 0 || validityDays > 3650)
            throw new ArgumentOutOfRangeException(nameof(validityDays), "ETA validity must be between 1 and 3650 days.");

        var issuedAt = now ?? DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddDays(validityDays);
        var verificationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(verificationToken)));
        var etaNumber = $"ETA-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";

        return (
            new Eta(Guid.NewGuid(), applicationId, etaNumber, hash, issuedAt, expiresAt),
            verificationToken);
    }

    public bool IsValidAt(DateTimeOffset now) =>
        Status == EtaStatus.Issued && now < ExpiresAtUtc;

    public bool MatchesVerificationToken(string verificationToken)
    {
        if (string.IsNullOrWhiteSpace(verificationToken))
            return false;

        var suppliedHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(verificationToken.Trim()));
        var storedHash = Convert.FromHexString(VerificationTokenHash);
        return CryptographicOperations.FixedTimeEquals(suppliedHash, storedHash);
    }

    public void Revoke(string reason, DateTimeOffset? now = null)
    {
        if (Status == EtaStatus.Revoked)
            throw new InvalidOperationException("ETA is already revoked.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A revocation reason is required.", nameof(reason));

        Status = EtaStatus.Revoked;
        RevokedAtUtc = now ?? DateTimeOffset.UtcNow;
        RevocationReason = reason.Trim();
    }
}
