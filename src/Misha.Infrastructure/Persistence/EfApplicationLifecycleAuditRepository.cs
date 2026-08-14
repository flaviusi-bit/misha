using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Domain.Applications;

namespace Misha.Infrastructure.Persistence;

public sealed class EfApplicationLifecycleAuditRepository(MishaDbContext db) : IApplicationLifecycleAuditRepository
{
    public Task AddAsync(ApplicationLifecycleAudit audit, CancellationToken cancellationToken)
    {
        db.ApplicationLifecycleAudits.Add(audit);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ApplicationLifecycleAudit>> GetByApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken) =>
        await db.ApplicationLifecycleAudits
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
}