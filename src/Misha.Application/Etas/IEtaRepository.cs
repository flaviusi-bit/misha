using Misha.Domain.Etas;

namespace Misha.Application.Etas;

public interface IEtaRepository
{
    Task<Eta?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken);
    Task<Eta?> GetByVerificationTokenHashAsync(string verificationTokenHash, CancellationToken cancellationToken);
    Task AddAsync(Eta eta, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
