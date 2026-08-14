using Misha.Domain.Applications;

namespace Misha.Application.Applications;

public interface IApplicationLifecycleAuditRepository
{
    Task AddAsync(ApplicationLifecycleAudit audit, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationLifecycleAudit>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken);
}
