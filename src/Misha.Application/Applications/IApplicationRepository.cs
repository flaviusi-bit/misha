using Misha.Domain.Applications;

namespace Misha.Application.Applications;

public interface IApplicationRepository
{
    Task<Misha.Domain.Applications.Application?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Misha.Domain.Applications.Application?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task AddAsync(Misha.Domain.Applications.Application application, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
