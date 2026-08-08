using System.Security.Cryptography;
using Misha.Application.Applications;
using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public sealed class DocumentService(
    IApplicationRepository applications,
    IDocumentArtifactRepository documents,
    IDocumentStorage storage)
{
    private const long MaxDocumentSizeBytes = 25 * 1024 * 1024;

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

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (content.CanSeek && content.Length <= 0)
            throw new ArgumentException("Document content is empty.", nameof(content));

        if (content.CanSeek && content.Length > MaxDocumentSizeBytes)
            throw new ArgumentException("Document exceeds the 25 MB upload limit.", nameof(content));

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length <= 0)
            throw new ArgumentException("Document content is empty.", nameof(content));

        if (buffer.Length > MaxDocumentSizeBytes)
            throw new ArgumentException("Document exceeds the 25 MB upload limit.", nameof(content));

        var bytes = buffer.ToArray();
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var storageKey = BuildStorageKey(applicationId, documentType, fileName);

        buffer.Position = 0;
        await storage.UploadAsync(storageKey, buffer, contentType, cancellationToken);

        try
        {
            var document = DocumentArtifact.Create(
                applicationId,
                documentType,
                fileName,
                contentType,
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

        var document = DocumentArtifact.Create(
            applicationId,
            documentType,
            fileName,
            contentType,
            sizeBytes,
            sha256,
            storageKey);

        await documents.AddAsync(document, cancellationToken);
        await documents.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<IReadOnlyList<DocumentArtifact>> GetAsync(
        Guid applicationId,
        CancellationToken cancellationToken) =>
        documents.GetByApplicationAsync(applicationId, cancellationToken);

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
