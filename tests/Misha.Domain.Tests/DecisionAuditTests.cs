using Misha.Domain.Decisions;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class DecisionAuditTests
{
    [Fact]
    public void Audit_requires_an_actor_and_records_immutable_decision_metadata()
    {
        var applicationId = Guid.NewGuid();

        var audit = DecisionAudit.Create(
            applicationId,
            "1.0",
            "Eligible",
            "Approve",
            "[]",
            "operator-123");

        Assert.NotEqual(Guid.Empty, audit.Id);
        Assert.Equal(applicationId, audit.ApplicationId);
        Assert.Equal("1.0", audit.PolicyVersion);
        Assert.Equal("Eligible", audit.PolicyDecision);
        Assert.Equal("Approve", audit.Decision);
        Assert.Equal("[]", audit.ReasonsJson);
        Assert.Equal("operator-123", audit.ActorReference);
        Assert.NotEqual(default, audit.CreatedAtUtc);
    }

    [Fact]
    public void Audit_rejects_missing_actor()
    {
        Assert.Throws<ArgumentException>(() => DecisionAudit.Create(
            Guid.NewGuid(),
            "1.0",
            "Eligible",
            "Approve",
            "[]",
            " "));
    }
}
