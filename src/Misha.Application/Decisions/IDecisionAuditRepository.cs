using Misha.Domain.Decisions;

namespace Misha.Application.Decisions;

public interface IDecisionAuditRepository
{
    Task AddAsync(DecisionAudit audit, CancellationToken cancellationToken);

    Task<IReadOnlyList<DecisionAudit>> GetByApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);
}
