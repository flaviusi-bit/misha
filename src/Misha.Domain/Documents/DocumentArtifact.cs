namespace Misha.Domain.Documents;

public sealed class DocumentArtifact
{
    private DocumentArtifact() { }

    private DocumentArtifact(
        Guid id,
        Guid applicationId,
        DocumentType documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        string sha256,
        string storageKey)
    {
        Id = id;
        ApplicationId = applicationId;
        DocumentType = documentType;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        StorageKey = storageKey;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static DocumentArtifact Create(
        Guid applicationId,
        DocumentType documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        string sha256,
        string storageKey)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));
        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size must be greater than zero.");
        if (string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("SHA-256 is required.", nameof(sha256));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        return new DocumentArtifact(
            Guid.NewGuid(),
            applicationId,
            documentType,
            fileName.Trim(),
            contentType.Trim(),
            sizeBytes,
            sha256.Trim().ToLowerInvariant(),
            storageKey.Trim());
    }
}
