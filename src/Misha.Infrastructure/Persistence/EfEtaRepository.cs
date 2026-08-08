using Microsoft.EntityFrameworkCore;
using Misha.Application.Etas;
using Misha.Domain.Etas;

namespace Misha.Infrastructure.Persistence;

public sealed class EfEtaRepository(MishaDbContext db) : IEtaRepository
{
    public Task<Eta?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken) =>
        db.Etas.FirstOrDefaultAsync(x => x.ApplicationId == applicationId, cancellationToken);

    public Task AddAsync(Eta eta, CancellationToken cancellationToken)
    {
        db.Etas.Add(eta);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
