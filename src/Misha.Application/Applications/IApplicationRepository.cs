using Misha.Domain.Applicants;
using Misha.Domain.Applications;
namespace Misha.Application.Applications;
public interface IApplicationRepository
{
    Task<Applicant> GetOrCreateApplicantAsync(string externalReference, string tenantId, CancellationToken cancellationToken);
    Task<Application?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Application?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<Application> AddOrGetExistingAsync(Application application, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
