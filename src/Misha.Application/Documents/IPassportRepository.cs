using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public interface IPassportRepository
{
    Task<PassportDocument?> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken);
    Task AddAsync(PassportDocument passport, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
