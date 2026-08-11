using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Misha.Application.Applications;
using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public sealed class DocumentService(
    IApplicationRepository applications,
    IDocumentArtifactRepository documents,
    IDocumentStorage storage)
{
    private const long MaxDocumentSizeBytes = 25 * 1024 * 1024;
    private const string Sha256Pattern = "^[0-9a-fA-F]{64}$";

    public async Task<DocumentArtifact> UploadAsync(
        Guid applicationId,
        DocumentType documentType,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        _ = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        ValidateFileMetadata(fileName, contentType);

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (content.CanSeek && content.Length <= 0)
            throw new ArgumentException("Document content is empty.", nameof(content));

        if (content.CanSeek && content.Length > MaxDocumentSizeBytes)
            throw new ArgumentException("Document exceeds the 25 MB upload limit.", nameof(content));

        await using var buffer = new MemoryStream(capacity: (int)Math.Min(MaxDocumentSizeBytes, 1024 * 1024));
        var copyBuffer = new byte[64 * 1024];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await content.ReadAsync(copyBuffer.AsMemory(), cancellationToken);
            if (bytesRead == 0)
                break;

            totalBytes += bytesRead;
            if (totalBytes > MaxDocumentSizeBytes)
                throw new ArgumentException("Document exceeds the 25 MB upload limit.", nameof(content));

            await buffer.WriteAsync(copyBuffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (totalBytes <= 0)
            throw new ArgumentException("Document content is empty.", nameof(content));

        var bytes = buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length));
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var storageKey = BuildStorageKey(applicationId, documentType, fileName);

        buffer.Position = 0;
        await storage.UploadAsync(storageKey, buffer, contentType, cancellationToken);

        try
        {
            var document = DocumentArtifact.Create(
                applicationId,
                documentType,
                fileName.Trim(),
                contentType.Trim(),
                bytes.Length,
                sha256,
                storageKey);

            await documents.AddAsync(document, cancellationToken);
            await documents.SaveChangesAsync(cancellationToken);
            return document;
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(storageKey, cancellationToken);
            }
            catch
            {
                // Preserve the original persistence failure. Orphan cleanup can be retried separately.
            }

            throw;
        }
    }

    public async Task<DocumentArtifact> RegisterAsync(
        Guid applicationId,
        DocumentType documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        string sha256,
        string storageKey,
        CancellationToken cancellationToken)
    {
        _ = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        ValidateFileMetadata(fileName, contentType);

        if (sizeBytes is <= 0 or > MaxDocumentSizeBytes)
            throw new ArgumentException("Document size must be between 1 byte and 25 MB.", nameof(sizeBytes));

        if (!Regex.IsMatch(sha256 ?? string.Empty, Sha256Pattern, RegexOptions.CultureInvariant))
            throw new ArgumentException("Document SHA-256 must be a 64-character hexadecimal value.", nameof(sha256));

        var expectedPrefix = $"applications/{applicationId:D}/documents/";
        if (string.IsNullOrWhiteSpace(storageKey) || !storageKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
            throw new ArgumentException("Document storage key must belong to the target application.", nameof(storageKey));

        var document = DocumentArtifact.Create(
            applicationId,
            documentType,
            fileName.Trim(),
            contentType.Trim(),
            sizeBytes,
            sha256.ToLowerInvariant(),
            storageKey);

        await documents.AddAsync(document, cancellationToken);
        await documents.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<IReadOnlyList<DocumentArtifact>> GetAsync(
        Guid applicationId,
        CancellationToken cancellationToken) =>
        documents.GetByApplicationAsync(applicationId, cancellationToken);

    private static void ValidateFileMetadata(string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        if (fileName.Length > 255)
            throw new ArgumentException("File name must not exceed 255 characters.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 127 || contentType.Contains('\r') || contentType.Contains('\n'))
            throw new ArgumentException("A valid content type is required.", nameof(contentType));
    }

    private static string BuildStorageKey(Guid applicationId, DocumentType documentType, string fileName)
    {
        var safeName = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        foreach (var invalid in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalid, '_');

        return $"applications/{applicationId:D}/documents/{documentType.ToString().ToLowerInvariant()}/{Guid.NewGuid():N}-{safeName}";
    }
}
