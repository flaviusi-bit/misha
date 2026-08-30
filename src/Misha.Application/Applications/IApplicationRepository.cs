using Misha.Domain.Applicants;
using DomainApplication = Misha.Domain.Applications.Application;
namespace Misha.Application.Applications;
public interface IApplicationRepository
{
    Task<Applicant> GetOrCreateApplicantAsync(string externalReference, string tenantId, CancellationToken cancellationToken);
    Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
