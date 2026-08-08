using Misha.Domain.Applications;

namespace Misha.Application.Applications;

public interface IApplicationRepository
{
    Task<Application?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Application application, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
