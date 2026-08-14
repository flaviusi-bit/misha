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

    public Task AddAsync(DomainApplication application, CancellationToken cancellationToken)
    {
        db.Applications.Add(application);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
