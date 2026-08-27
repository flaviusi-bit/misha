using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public interface IDocumentArtifactRepository
{
    Task<IReadOnlyList<DocumentArtifact>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken);
    Task AddAsync(DocumentArtifact document, CancellationToken cancellationToken);
    Task DeleteAsync(DocumentArtifact document, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
