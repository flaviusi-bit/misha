using ApplicantEntity = Misha.Domain.Applicants.Applicant;
using ApplicationEntity = Misha.Domain.Applications.Application;

namespace Misha.Application.Applications;

public interface IApplicationRepository
{
    Task<ApplicantEntity> GetOrCreateApplicantAsync(string externalReference, CancellationToken cancellationToken);
    Task<ApplicationEntity?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ApplicationEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<ApplicationEntity> AddOrGetExistingAsync(ApplicationEntity application, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
