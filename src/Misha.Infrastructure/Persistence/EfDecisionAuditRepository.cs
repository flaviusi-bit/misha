using Microsoft.EntityFrameworkCore;
using Misha.Application.Decisions;
using Misha.Domain.Decisions;

namespace Misha.Infrastructure.Persistence;

public sealed class EfDecisionAuditRepository(MishaDbContext db) : IDecisionAuditRepository
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
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
