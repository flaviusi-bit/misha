using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using DomainApplication = Misha.Domain.Applications.Application;

namespace Misha.Infrastructure.Persistence;

public sealed class EfApplicationRepository(MishaDbContext db) : IApplicationRepository
{
    public Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Applications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        db.Applications.SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken)
    {
        db.Applications.Add(application);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return application;
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(application.IdempotencyKey))
        {
            var existing = await db.Applications.SingleOrDefaultAsync(
                x => x.IdempotencyKey == application.IdempotencyKey,
                cancellationToken);

            if (existing is not null)
                return existing;

            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
