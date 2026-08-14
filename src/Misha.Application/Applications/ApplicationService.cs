namespace Misha.Application.Applications;

public sealed class ApplicationService(IApplicationRepository repository)
{
    public async Task<Guid> CreateAsync(string applicantReference, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await repository.GetByIdempotencyKeyAsync(idempotencyKey.Trim(), cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.ApplicantReference, applicantReference.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("The idempotency key has already been used for a different application request.");

                return existing.Id;
            }
        }

        var application = Misha.Domain.Applications.Application.Create(applicantReference, idempotencyKey);
        var persisted = await repository.AddOrGetExistingAsync(application, cancellationToken);

        if (!string.Equals(persisted.ApplicantReference, applicantReference.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("The idempotency key has already been used for a different application request.");

        return persisted.Id;
    }

    public Task<Guid> CreateAsync(string applicantReference, CancellationToken cancellationToken) =>
        CreateAsync(applicantReference, null, cancellationToken);

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
