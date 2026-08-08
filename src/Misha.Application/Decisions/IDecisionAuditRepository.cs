using Misha.Domain.Decisions;

namespace Misha.Application.Decisions;

public interface IDecisionAuditRepository
{
    Task AddAsync(DecisionAudit audit, CancellationToken cancellationToken);
}
