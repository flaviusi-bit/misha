using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Domain.Documents;
using Xunit;
using DomainApplication = Misha.Domain.Applications.Application;

namespace Misha.Domain.Tests;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task UploadAsync_rejects_streams_larger_than_limit()
    {
        var applicationId = Guid.NewGuid();
        var service = CreateService(applicationId, out _, out _);
        await using var content = new MemoryStream(new byte[25 * 1024 * 1024 + 1]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", content, CancellationToken.None));

        Assert.Contains("25 MB", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_rejects_invalid_sha256()
    {
        var applicationId = Guid.NewGuid();
        var service = CreateService(applicationId, out _, out _);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(
                applicationId,
                DocumentType.Passport,
                "passport.pdf",
                "application/pdf",
                100,
                "not-a-sha",
                $"applications/{applicationId:D}/documents/passport/passport.pdf",
                CancellationToken.None));

        Assert.Contains("SHA-256", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_rejects_storage_key_for_another_application()
    {
        var applicationId = Guid.NewGuid();
        var service = CreateService(applicationId, out _, out _);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(
                applicationId,
                DocumentType.Passport,
                "passport.pdf",
                "application/pdf",
                100,
                new string('a', 64),
                $"applications/{Guid.NewGuid():D}/documents/passport/passport.pdf",
                CancellationToken.None));

        Assert.Contains("storage key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DocumentService CreateService(
        Guid applicationId,
        out FakeDocumentRepository documents,
        out FakeDocumentStorage storage)
    {
        documents = new FakeDocumentRepository();
        storage = new FakeDocumentStorage();
        return new DocumentService(
            new FakeApplicationRepository(applicationId),
            documents,
            storage);
    }

    private sealed class FakeApplicationRepository(Guid existingId) : IApplicationRepository
    {
        public Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DomainApplication?>(id == existingId ? DomainApplication.Create("test") : null);

        public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<DomainApplication?>(null);

        public Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken) =>
            Task.FromResult(application);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDocumentRepository : IDocumentArtifactRepository
    {
        public List<DocumentArtifact> Items { get; } = [];
        public Task AddAsync(DocumentArtifact document, CancellationToken cancellationToken) { Items.Add(document); return Task.CompletedTask; }
        public Task<IReadOnlyList<DocumentArtifact>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentArtifact>>(Items);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
