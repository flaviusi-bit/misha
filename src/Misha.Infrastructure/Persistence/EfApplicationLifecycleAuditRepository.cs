using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Application.Tenants;
using Misha.Domain.Applications;

namespace Misha.Infrastructure.Persistence;

public sealed class EfApplicationLifecycleAuditRepository(
    MishaDbContext db,
    ITenantContext tenantContext) : IApplicationLifecycleAuditRepository
{
    public Task AddAsync(ApplicationLifecycleAudit audit, CancellationToken cancellationToken)
    {
        db.ApplicationLifecycleAudits.Add(audit);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ApplicationLifecycleAudit>> GetByApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var query = db.ApplicationLifecycleAudits
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId);

        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
            {
                return [];
            }

            query = query.Where(x => db.Applications
                .Any(a => a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId));
        }

        return await query
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }
}