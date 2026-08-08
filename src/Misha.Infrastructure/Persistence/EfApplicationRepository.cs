using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Domain.Applications;

namespace Misha.Infrastructure.Persistence;

public sealed class EfApplicationRepository(MishaDbContext db) : IApplicationRepository
{
    public Task<Application?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Applications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(Application application, CancellationToken cancellationToken)
    {
        db.Applications.Add(application);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
