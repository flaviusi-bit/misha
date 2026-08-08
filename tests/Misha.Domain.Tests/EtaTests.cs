using Misha.Domain.Etas;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class EtaTests
{
    [Fact]
    public void Issue_creates_issued_eta_and_verification_token()
    {
        var issuedAt = new DateTimeOffset(2026, 8, 8, 20, 0, 0, TimeSpan.Zero);

        var (eta, token) = Eta.Issue(Guid.NewGuid(), 90, issuedAt);

        Assert.Equal(EtaStatus.Issued, eta.Status);
        Assert.StartsWith("ETA-", eta.EtaNumber, StringComparison.Ordinal);
        Assert.NotEmpty(token);
        Assert.Equal(issuedAt.AddDays(90), eta.ExpiresAtUtc);
        Assert.True(eta.MatchesVerificationToken(token));
    }

    [Fact]
    public void Verification_token_is_not_stored_in_plain_text()
    {
        var (eta, token) = Eta.Issue(Guid.NewGuid(), 90);

        Assert.NotEqual(token, eta.VerificationTokenHash);
        Assert.Equal(eta.VerificationTokenHash, Eta.HashVerificationToken(token));
        Assert.True(eta.MatchesVerificationToken(token));
        Assert.False(eta.MatchesVerificationToken("wrong-token"));
    }

    [Fact]
    public void Verification_token_hash_is_deterministic()
    {
        const string token = "test-verification-token";

        Assert.Equal(Eta.HashVerificationToken(token), Eta.HashVerificationToken(token));
        Assert.NotEqual(Eta.HashVerificationToken(token), Eta.HashVerificationToken("another-token"));
    }

    [Fact]
    public void Issuance_rejects_invalid_validity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Eta.Issue(Guid.NewGuid(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Eta.Issue(Guid.NewGuid(), 3651));
    }

    [Fact]
    public void Eta_expires_when_now_reaches_expiry()
    {
        var issuedAt = new DateTimeOffset(2026, 8, 8, 20, 0, 0, TimeSpan.Zero);
        var (eta, _) = Eta.Issue(Guid.NewGuid(), 1, issuedAt);

        Assert.True(eta.IsValidAt(issuedAt.AddHours(23)));
        Assert.False(eta.IsValidAt(issuedAt.AddDays(1)));
    }

    [Fact]
    public void Revoke_requires_reason_and_marks_eta_revoked()
    {
        var issuedAt = new DateTimeOffset(2026, 8, 8, 20, 0, 0, TimeSpan.Zero);
        var (eta, _) = Eta.Issue(Guid.NewGuid(), 90, issuedAt);

        eta.Revoke("Application eligibility changed.", issuedAt.AddDays(2));

        Assert.Equal(EtaStatus.Revoked, eta.Status);
        Assert.Equal(issuedAt.AddDays(2), eta.RevokedAtUtc);
        Assert.Equal("Application eligibility changed.", eta.RevocationReason);
        Assert.False(eta.IsValidAt(issuedAt.AddDays(3)));
    }
}
