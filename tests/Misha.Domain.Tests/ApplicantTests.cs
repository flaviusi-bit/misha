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
        Assert.False(applicant.ProfileCompleted);
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

    [Fact]
    public void SetProfile_normalizes_core_identity_and_marks_profile_complete()
    {
        var applicant = Applicant.Create("traveller-001");

        applicant.SetProfile(new ApplicantProfile(
            "  Maria ",
            " Popescu ",
            new DateOnly(1990, 5, 12),
            "ro",
            "ro",
            "Bucharest",
            "female",
            "maria@example.com",
            "+40700000000"));

        Assert.True(applicant.ProfileCompleted);
        Assert.Equal("Maria", applicant.FirstName);
        Assert.Equal("Popescu", applicant.LastName);
        Assert.Equal(new DateOnly(1990, 5, 12), applicant.DateOfBirth);
        Assert.Equal("RO", applicant.Nationality);
        Assert.Equal("RO", applicant.CountryOfBirth);
        Assert.Equal("Bucharest", applicant.PlaceOfBirth);
        Assert.Equal("female", applicant.Gender);
        Assert.Equal("maria@example.com", applicant.Email);
        Assert.Equal("+40700000000", applicant.PhoneNumber);
        Assert.NotNull(applicant.UpdatedAtUtc);
    }

    [Fact]
    public void SetProfile_rejects_future_date_of_birth()
    {
        var applicant = Applicant.Create("traveller-001");

        Assert.Throws<ArgumentException>(() => applicant.SetProfile(new ApplicantProfile(
            "Maria",
            "Popescu",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            "RO",
            null,
            null,
            null,
            null,
            null)));
    }

    [Fact]
    public void SetProfile_requires_core_identity_fields()
    {
        var applicant = Applicant.Create("traveller-001");

        Assert.Throws<ArgumentException>(() => applicant.SetProfile(new ApplicantProfile(
            "",
            "Popescu",
            new DateOnly(1990, 5, 12),
            "RO",
            null,
            null,
            null,
            null,
            null)));
    }
}
