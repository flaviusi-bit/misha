using Misha.Domain.Applications;

namespace Misha.Application.Applications;

public sealed class ApplicationService(IApplicationRepository repository)
{
    public async Task<Guid> CreateAsync(string applicantReference, CancellationToken cancellationToken)
    {
        var application = Misha.Domain.Applications.Application.Create(applicantReference);
        await repository.AddAsync(application, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return application.Id;
    }

    public async Task SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{id}' was not found.");

        application.Submit();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
