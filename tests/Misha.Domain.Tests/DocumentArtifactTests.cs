using Misha.Domain.Documents;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class DocumentArtifactTests
{
    [Fact]
    public void Create_normalizes_sha256_and_trims_metadata()
    {
        var applicationId = Guid.NewGuid();

        var document = DocumentArtifact.Create(
            applicationId,
            DocumentType.Passport,
            " passport.jpg ",
            " image/jpeg ",
            1024,
            " ABCDEF1234 ",
            " applications/test/passport.jpg ");

        Assert.Equal(applicationId, document.ApplicationId);
        Assert.Equal("passport.jpg", document.FileName);
        Assert.Equal("image/jpeg", document.ContentType);
        Assert.Equal("abcdef1234", document.Sha256);
        Assert.Equal("applications/test/passport.jpg", document.StorageKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_non_positive_size(long sizeBytes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentArtifact.Create(
            Guid.NewGuid(),
            DocumentType.Passport,
            "passport.jpg",
            "image/jpeg",
            sizeBytes,
            "abc",
            "key"));
    }
}
