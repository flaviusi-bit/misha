using Misha.Domain.Documents;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PassportDocumentTests
{
    [Fact]
    public void Create_normalizes_document_identity_fields()
    {
        var applicationId = Guid.NewGuid();

        var passport = PassportDocument.Create(
            applicationId,
            "  ab123456  ",
            " ro ",
            "Popescu",
            "Ion Andrei",
            new DateOnly(1985, 4, 12),
            "ro",
            new DateOnly(2032, 4, 11));

        Assert.Equal(applicationId, passport.ApplicationId);
        Assert.Equal("AB123456", passport.DocumentNumber);
        Assert.Equal("RO", passport.IssuingCountry);
        Assert.Equal("RO", passport.Nationality);
        Assert.False(passport.IsExpired(new DateOnly(2026, 8, 8)));
    }

    [Fact]
    public void Expired_passport_is_detected()
    {
        var passport = PassportDocument.Create(
            Guid.NewGuid(),
            "AB123456",
            "RO",
            "Popescu",
            "Ion",
            new DateOnly(1985, 4, 12),
            "RO",
            new DateOnly(2026, 8, 7));

        Assert.True(passport.IsExpired(new DateOnly(2026, 8, 8)));
    }

    [Fact]
    public void Missing_document_number_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => PassportDocument.Create(
            Guid.NewGuid(),
            " ",
            "RO",
            "Popescu",
            "Ion",
            new DateOnly(1985, 4, 12),
            "RO",
            new DateOnly(2032, 4, 11)));
    }
}
