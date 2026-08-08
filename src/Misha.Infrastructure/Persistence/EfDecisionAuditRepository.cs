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
}
