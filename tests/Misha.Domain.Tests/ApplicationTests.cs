using Misha.Domain.Applications;
using Xunit;
using DomainApplication = Misha.Domain.Applications.Application;

namespace Misha.Domain.Tests;

public sealed class ApplicationTests
{
    [Fact]
    public void Create_starts_as_draft()
    {
        var applicantId = Guid.NewGuid();
        var application = DomainApplication.Create(applicantId, "traveller-001");

        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Equal(applicantId, application.ApplicantId);
        Assert.Equal("traveller-001", application.ApplicantReference);
    }

    [Fact]
    public void Create_rejects_empty_applicant_id()
    {
        Assert.Throws<ArgumentException>(() => DomainApplication.Create(Guid.Empty, "traveller-001"));
    }

    [Fact]
    public void Submit_moves_draft_to_submitted()
    {
        var application = DomainApplication.Create(Guid.NewGuid(), "traveller-001");
        application.Submit();
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.NotNull(application.SubmittedAtUtc);
    }

    [Fact]
    public void Submit_cannot_be_called_twice()
    {
        var application = DomainApplication.Create(Guid.NewGuid(), "traveller-001");
        application.Submit();
        Assert.Throws<InvalidOperationException>(() => application.Submit());
    }

    [Fact]
    public void Processing_moves_submitted_to_processing()
    {
        var application = DomainApplication.Create(Guid.NewGuid(), "traveller-001");
        application.Submit();
        application.StartProcessing();
        Assert.Equal(ApplicationStatus.Processing, application.Status);
        Assert.NotNull(application.ProcessingStartedAtUtc);
    }

    [Fact]
    public void Approve_moves_processing_to_approved()
    {
        var application = CreateProcessingApplication();
        application.Approve();
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.NotNull(application.DecidedAtUtc);
        Assert.Null(application.RefusalReason);
    }

    [Fact]
    public void Refuse_requires_a_reason_and_records_it()
    {
        var application = CreateProcessingApplication();
        application.Refuse("Watchlist match requires refusal.");
        Assert.Equal(ApplicationStatus.Refused, application.Status);
        Assert.Equal("Watchlist match requires refusal.", application.RefusalReason);
        Assert.NotNull(application.DecidedAtUtc);
    }

    [Fact]
    public void Refuse_without_reason_is_rejected()
    {
        var application = CreateProcessingApplication();
        Assert.Throws<ArgumentException>(() => application.Refuse("  "));
    }

    [Fact]
    public void Cancel_is_allowed_before_decision()
    {
        var application = CreateProcessingApplication();
        application.Cancel();
        Assert.Equal(ApplicationStatus.Cancelled, application.Status);
        Assert.NotNull(application.CancelledAtUtc);
    }

    [Fact]
    public void Approved_application_cannot_be_cancelled()
    {
        var application = CreateProcessingApplication();
        application.Approve();
        Assert.Throws<InvalidOperationException>(() => application.Cancel());
    }

    [Fact]
    public void Processing_cannot_start_before_submission()
    {
        var application = DomainApplication.Create(Guid.NewGuid(), "traveller-001");
        Assert.Throws<InvalidOperationException>(() => application.StartProcessing());
    }

    private static DomainApplication CreateProcessingApplication()
    {
        var application = DomainApplication.Create(Guid.NewGuid(), "traveller-001");
        application.Submit();
        application.StartProcessing();
        return application;
    }
}
