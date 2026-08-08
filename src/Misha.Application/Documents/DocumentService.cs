using Misha.Application.Applications;
using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public sealed class DocumentService(
    IApplicationRepository applications,
    IDocumentArtifactRepository documents)
{
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
}
