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

    public Task<Misha.Domain.Applications.Application?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetAsync(id, cancellationToken);

    public async Task SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await GetRequiredAsync(id, cancellationToken);
        application.Submit();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task StartProcessingAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await GetRequiredAsync(id, cancellationToken);
        application.StartProcessing();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await GetRequiredAsync(id, cancellationToken);
        application.Approve();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task RefuseAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        var application = await GetRequiredAsync(id, cancellationToken);
        application.Refuse(reason);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await GetRequiredAsync(id, cancellationToken);
        application.Cancel();
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Misha.Domain.Applications.Application> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{id}' was not found.");
    }
}
