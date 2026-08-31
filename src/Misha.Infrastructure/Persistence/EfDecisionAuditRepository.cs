using Microsoft.EntityFrameworkCore;
using Misha.Application.Decisions;
using Misha.Application.Tenants;
using Misha.Domain.Decisions;

namespace Misha.Infrastructure.Persistence;

public sealed class EfDecisionAuditRepository(MishaDbContext db, ITenantContext tenantContext) : IDecisionAuditRepository
{
    public Task AddAsync(DecisionAudit audit, CancellationToken cancellationToken)
    {
        db.DecisionAudits.Add(audit);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DecisionAudit>> GetByApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return await db.DecisionAudits
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId &&
                        (tenantContext.IsAdmin || db.Applications.Any(a =>
                            a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
