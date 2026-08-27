using System.Security.Cryptography;
using Misha.Application.Applications;
using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public sealed class DocumentService(
    IApplicationRepository applications,
    IDocumentArtifactRepository documents,
    IDocumentStorage storage,
    IContentSafetyScanner contentSafetyScanner)
{
    private const long MaxDocumentSizeBytes = 25 * 1024 * 1024;
    private const int CopyBufferSize = 64 * 1024;
    private static readonly TimeSpan PreSignedUrlLifetime = TimeSpan.FromMinutes(10);

    public async Task<DocumentArtifact> UploadAsync(
        Guid applicationId, DocumentType documentType, string fileName, string contentType,
        Stream content, CancellationToken cancellationToken)
    {
        _ = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");
        ValidateFileMetadata(fileName, contentType);
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.CanSeek && content.Length <= 0) throw new ArgumentException("Document content is empty.", nameof(content));
        if (content.CanSeek && content.Length > MaxDocumentSizeBytes) throw new ArgumentException("Document exceeds the 25 MB upload limit.", nameof(content));

        await using var buffer = new MemoryStream(capacity: (int)Math.Min(MaxDocumentSizeBytes, 1024 * 1024));
        var copyBuffer = new byte[CopyBufferSize];
        long totalBytes = 0;
        while (true)
        {
            var bytesRead = await content.ReadAsync(copyBuffer.AsMemory(), cancellationToken);
            if (bytesRead == 0) break;
            totalBytes += bytesRead;
            if (totalBytes > MaxDocumentSizeBytes) throw new ArgumentException("Document exceeds the 25 MB upload limit.", nameof(content));
            await buffer.WriteAsync(copyBuffer.AsMemory(0, bytesRead), cancellationToken);
        }
        if (totalBytes <= 0) throw new ArgumentException("Document content is empty.", nameof(content));

        buffer.Position = 0;
        var safety = await contentSafetyScanner.ScanAsync(buffer, fileName.Trim(), contentType.Trim(), cancellationToken);
        if (!safety.Allowed)
            throw new ArgumentException(safety.Reason ?? "Document content was rejected by the safety validation boundary.", nameof(content));

        var bytes = buffer.ToArray();
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var storageKey = BuildStorageKey(applicationId, documentType, fileName);
        buffer.Position = 0;
        await storage.UploadAsync(storageKey, buffer, contentType.Trim(), cancellationToken);
        try
        {
            var document = DocumentArtifact.Create(applicationId, documentType, fileName.Trim(), contentType.Trim(), bytes.LongLength, sha256, storageKey);
            await documents.AddAsync(document, cancellationToken);
            await documents.SaveChangesAsync(cancellationToken);
            return document;
        }
        catch
        {
            try { await storage.DeleteAsync(storageKey, cancellationToken); } catch { }
            throw;
        }
    }

    public async Task<DocumentArtifact> RegisterAsync(
        Guid applicationId, DocumentType documentType, string fileName, string contentType,
        long sizeBytes, string sha256, string storageKey, CancellationToken cancellationToken)
    {
        _ = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");
        ValidateFileMetadata(fileName, contentType);
        if (sizeBytes is <= 0 or > MaxDocumentSizeBytes) throw new ArgumentException("Document size must be between 1 byte and 25 MB.", nameof(sizeBytes));
        if (!IsValidSha256(sha256)) throw new ArgumentException("Document SHA-256 must be a 64-character hexadecimal value.", nameof(sha256));
        var normalizedSha256 = sha256.ToLowerInvariant();
        var expectedPrefix = $"applications/{applicationId:D}/documents/";
        if (string.IsNullOrWhiteSpace(storageKey) || !storageKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
            throw new ArgumentException("Document storage key must belong to the target application.", nameof(storageKey));

        var document = DocumentArtifact.Create(applicationId, documentType, fileName.Trim(), contentType.Trim(), sizeBytes, normalizedSha256, storageKey);
        await documents.AddAsync(document, cancellationToken);
        await documents.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<IReadOnlyList<DocumentArtifact>> GetAsync(Guid applicationId, CancellationToken cancellationToken) =>
        documents.GetByApplicationAsync(applicationId, cancellationToken);

    public async Task<(string StorageKey, Uri Url)> CreatePreSignedUploadAsync(
        Guid applicationId, DocumentType documentType, string fileName, string contentType, CancellationToken cancellationToken)
    {
        _ = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");
        ValidateFileMetadata(fileName, contentType);
        var storageKey = BuildStorageKey(applicationId, documentType, fileName);
        return (storageKey, storage.CreatePreSignedUploadUrl(storageKey, contentType.Trim(), PreSignedUrlLifetime));
    }

    public async Task<Uri> CreatePreSignedDownloadAsync(Guid applicationId, Guid documentId, CancellationToken cancellationToken)
    {
        _ = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");
        var document = (await documents.GetByApplicationAsync(applicationId, cancellationToken))
            .SingleOrDefault(x => x.Id == documentId)
            ?? throw new KeyNotFoundException($"Document '{documentId}' was not found for application '{applicationId}'.");
        return storage.CreatePreSignedDownloadUrl(document.StorageKey, PreSignedUrlLifetime);
    }

    private static void ValidateFileMetadata(string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (fileName.Length > 255) throw new ArgumentException("File name must not exceed 255 characters.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 127 || contentType.Contains('\r') || contentType.Contains('\n'))
            throw new ArgumentException("A valid content type is required.", nameof(contentType));
    }

    private static bool IsValidSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static string BuildStorageKey(Guid applicationId, DocumentType documentType, string fileName)
    {
        var safeName = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrWhiteSpace(safeName)) throw new ArgumentException("File name is required.", nameof(fileName));
        foreach (var invalid in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(invalid, '_');
        return $"applications/{applicationId:D}/documents/{documentType.ToString().ToLowerInvariant()}/{Guid.NewGuid():N}-{safeName}";
    }
}
