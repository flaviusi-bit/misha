using Misha.Domain.Etas;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class EtaAuditTests
{
    [Fact]
    public void Audit_records_issued_event_without_sensitive_token_data()
    {
        var etaId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 8, 22, 0, 0, TimeSpan.Zero);

        var audit = EtaAudit.Create(
            EtaAuditEventType.Issued,
            "Success",
            "system",
            etaId,
            applicationId,
            "ETA-ABC123",
            occurredAt);

        Assert.Equal(EtaAuditEventType.Issued, audit.EventType);
        Assert.Equal("Success", audit.Outcome);
        Assert.Equal("system", audit.ActorReference);
        Assert.Equal(etaId, audit.EtaId);
        Assert.Equal(applicationId, audit.ApplicationId);
        Assert.Equal("ETA-ABC123", audit.EtaNumber);
        Assert.Equal(occurredAt, audit.OccurredAtUtc);
    }

    [Fact]
    public void Verification_failure_can_be_audited_without_resolving_an_eta()
    {
        var audit = EtaAudit.Create(
            EtaAuditEventType.VerificationFailed,
            "NotFound",
            "public-verification");

        Assert.Equal(EtaAuditEventType.VerificationFailed, audit.EventType);
        Assert.Equal("NotFound", audit.Outcome);
        Assert.Equal("public-verification", audit.ActorReference);
        Assert.Null(audit.EtaId);
        Assert.Null(audit.ApplicationId);
        Assert.Null(audit.EtaNumber);
    }

    [Fact]
    public void Audit_requires_an_actor_reference()
    {
        Assert.Throws<ArgumentException>(() => EtaAudit.Create(
            EtaAuditEventType.Verified,
            "Valid",
            " ",
            etaNumber: "ETA-ABC123"));
    }
}
