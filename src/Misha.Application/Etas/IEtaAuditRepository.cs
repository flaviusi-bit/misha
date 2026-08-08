using Misha.Domain.Etas;

namespace Misha.Application.Etas;

public interface IEtaAuditRepository
{
    Task AddAsync(EtaAudit audit, CancellationToken cancellationToken);
}
