using Misha.Domain.Applicants;

namespace Misha.Application.Applicants;

public interface IApplicantRepository
{
    Task<Applicant?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
