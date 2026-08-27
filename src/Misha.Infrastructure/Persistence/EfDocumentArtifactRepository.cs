using Microsoft.EntityFrameworkCore;
using Misha.Application.Documents;
using Misha.Domain.Documents;

namespace Misha.Infrastructure.Persistence;

public sealed class EfDocumentArtifactRepository(MishaDbContext db) : IDocumentArtifactRepository
{
    public async Task<IReadOnlyList<DocumentArtifact>> GetByApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await db.DocumentArtifacts
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task AddAsync(DocumentArtifact document, CancellationToken cancellationToken)
    {
        db.DocumentArtifacts.Add(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentArtifact document, CancellationToken cancellationToken)
    {
        db.DocumentArtifacts.Remove(document);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
