using Misha.Domain.Applicants;

namespace Misha.Application.Applicants;

public sealed class ApplicantService(IApplicantRepository repository)
{
    public Task<Applicant?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetAsync(id, cancellationToken);

    public async Task UpdateProfileAsync(
        Guid id,
        ApplicantProfile profile,
        CancellationToken cancellationToken)
    {
        var applicant = await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Applicant '{id}' was not found.");

        applicant.SetProfile(profile);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
