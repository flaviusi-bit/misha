using Misha.Domain.Applications;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class ApplicationTests
{
    [Fact]
    public void Create_starts_as_draft()
    {
        var application = Application.Create("traveller-001");

        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Equal("traveller-001", application.ApplicantReference);
    }

    [Fact]
    public void Submit_moves_draft_to_submitted()
    {
        var application = Application.Create("traveller-001");

        application.Submit();

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.NotNull(application.SubmittedAtUtc);
    }

    [Fact]
    public void Submit_cannot_be_called_twice()
    {
        var application = Application.Create("traveller-001");
        application.Submit();

        Assert.Throws<InvalidOperationException>(() => application.Submit());
    }
}
