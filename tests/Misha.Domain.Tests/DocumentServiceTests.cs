using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Domain.Applicants;
using Misha.Domain.Documents;
using Xunit;
using DomainApplication = Misha.Domain.Applications.Application;

namespace Misha.Domain.Tests;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task UploadAsync_rejects_streams_larger_than_limit()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out _, out _);
        await using var content = new MemoryStream(new byte[25 * 1024 * 1024 + 1]);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", content, CancellationToken.None));
        Assert.Contains("25 MB", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAsync_rejects_content_when_safety_scanner_blocks_it()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out _, out _, new BlockingScanner());
        await using var content = new MemoryStream("%PDF-1.7"u8.ToArray());
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", content, CancellationToken.None));
        Assert.Contains("safety", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_does_not_store_content_when_safety_scanner_blocks_it()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out _, out var storage, new BlockingScanner());
        await using var content = new MemoryStream("%PDF-1.7"u8.ToArray());
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", content, CancellationToken.None));
        Assert.False(storage.Uploaded);
    }

    [Fact]
    public async Task RegisterAsync_rejects_invalid_sha256()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out _, out _);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", 100, "not-a-sha", $"applications/{applicationId:D}/documents/passport/passport.pdf", CancellationToken.None));
        Assert.Contains("SHA-256", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_rejects_storage_key_for_another_application()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out _, out _);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", 100, new string('a', 64), $"applications/{Guid.NewGuid():D}/documents/passport/passport.pdf", CancellationToken.None));
        Assert.Contains("storage key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_removes_metadata_and_storage()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out var documents, out var storage);
        var document = DocumentArtifact.Create(applicationId, DocumentType.Passport, "passport.pdf", "application/pdf", 100, new string('a', 64), $"applications/{applicationId:D}/documents/passport/test.pdf");
        documents.Items.Add(document);
        await service.DeleteAsync(applicationId, document.Id, CancellationToken.None);
        Assert.Empty(documents.Items);
        Assert.Equal(document.StorageKey, storage.DeletedKey);
    }

    [Fact]
    public async Task DeleteAsync_is_idempotent_for_missing_document()
    {
        var applicationId = Guid.NewGuid(); var service = CreateService(applicationId, out var documents, out var storage);
        await service.DeleteAsync(applicationId, Guid.NewGuid(), CancellationToken.None);
        Assert.Empty(documents.Items);
        Assert.Null(storage.DeletedKey);
    }

    private static DocumentService CreateService(Guid applicationId, out FakeDocumentRepository documents, out FakeDocumentStorage storage, IContentSafetyScanner? scanner = null)
    {
        documents = new FakeDocumentRepository(); storage = new FakeDocumentStorage();
        return new DocumentService(new FakeApplicationRepository(applicationId), documents, storage, scanner ?? new AllowingScanner());
    }
    private sealed class AllowingScanner : IContentSafetyScanner
    { public Task<ContentSafetyResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken) => Task.FromResult(new ContentSafetyResult(true)); }
    private sealed class BlockingScanner : IContentSafetyScanner
    { public Task<ContentSafetyResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken) => Task.FromResult(new ContentSafetyResult(false, "blocked by safety scanner")); }
    private sealed class FakeApplicationRepository(Guid existingId) : IApplicationRepository
    {
        public Task<Applicant> GetOrCreateApplicantAsync(string externalReference, CancellationToken cancellationToken) => Task.FromResult(Applicant.Create(externalReference));
        public Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<DomainApplication?>(id == existingId ? DomainApplication.Create("test") : null);
        public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult<DomainApplication?>(null);
        public Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken) => Task.FromResult(application);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class FakeDocumentRepository : IDocumentArtifactRepository
    {
        public List<DocumentArtifact> Items { get; } = [];
        public Task AddAsync(DocumentArtifact document, CancellationToken cancellationToken) { Items.Add(document); return Task.CompletedTask; }
        public Task DeleteAsync(DocumentArtifact document, CancellationToken cancellationToken) { Items.Remove(document); return Task.CompletedTask; }
        public Task<IReadOnlyList<DocumentArtifact>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentArtifact>>(Items.Where(x => x.ApplicationId == applicationId).ToList());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        public bool Uploaded { get; private set; }
        public string? DeletedKey { get; private set; }
        public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken) { Uploaded = true; return Task.CompletedTask; }
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(new byte[100]));
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) { DeletedKey = storageKey; return Task.CompletedTask; }
        public Uri CreatePreSignedUploadUrl(string storageKey, string contentType, TimeSpan lifetime) => new($"https://example.test/{storageKey}");
        public Uri CreatePreSignedDownloadUrl(string storageKey, TimeSpan lifetime) => new($"https://example.test/{storageKey}");
    }
}
