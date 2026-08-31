using Microsoft.EntityFrameworkCore;
using Misha.Application.Etas;
using Misha.Application.Tenants;
using Misha.Domain.Etas;

namespace Misha.Infrastructure.Persistence;

public sealed class EfEtaRepository(MishaDbContext db, ITenantContext tenantContext) : IEtaRepository
{
    public Task<Eta?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken) =>
        db.Etas.FirstOrDefaultAsync(x => x.ApplicationId == applicationId &&
                                         (tenantContext.IsAdmin || db.Applications.Any(a =>
                                             a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId)), cancellationToken);

    public Task<Eta?> GetByVerificationTokenHashAsync(string verificationTokenHash, CancellationToken cancellationToken) =>
        db.Etas.FirstOrDefaultAsync(x => x.VerificationTokenHash == verificationTokenHash, cancellationToken);

    public Task AddAsync(Eta eta, CancellationToken cancellationToken)
    {
        db.Etas.Add(eta);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
