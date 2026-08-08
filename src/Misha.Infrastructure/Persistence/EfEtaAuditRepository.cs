using Misha.Application.Etas;
using Misha.Domain.Etas;

namespace Misha.Infrastructure.Persistence;

public sealed class EfEtaAuditRepository(MishaDbContext db) : IEtaAuditRepository
{
    public Task AddAsync(EtaAudit audit, CancellationToken cancellationToken)
    {
        db.EtaAudits.Add(audit);
        return Task.CompletedTask;
    }
}
