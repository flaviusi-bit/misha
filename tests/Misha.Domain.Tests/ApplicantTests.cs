using Misha.Domain.Applicants;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class ApplicantTests
{
    [Fact]
    public void Create_normalizes_external_reference()
    {
        var applicant = Applicant.Create("  traveller-001  ");

        Assert.NotEqual(Guid.Empty, applicant.Id);
        Assert.Equal("traveller-001", applicant.ExternalReference);
        Assert.NotEqual(default, applicant.CreatedAtUtc);
    }

    [Fact]
    public void Create_rejects_empty_reference()
    {
        Assert.Throws<ArgumentException>(() => Applicant.Create("  "));
    }

    [Fact]
    public void Create_rejects_reference_over_200_characters()
    {
        Assert.Throws<ArgumentException>(() => Applicant.Create(new string('x', 201)));
    }
}
