using Misha.Domain.Applications;

namespace Misha.Application.Applications;

public sealed class ApplicationService(IApplicationRepository repository, IApplicationLifecycleAuditRepository lifecycleAudits)
{
    public Task<Guid> CreateAsync(string applicantReference, string? idempotencyKey, string actorReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));

        return CreateAsync(applicantReference, idempotencyKey, cancellationToken);
    }

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

    public Task<Misha.Domain.Applications.Application?> GetAsync(Guid id, CancellationToken cancellationToken) => repository.GetAsync(id, cancellationToken);

    public Task SubmitAsync(Guid id, string actorReference, CancellationToken cancellationToken) => TransitionAsync(id, actorReference, application => application.Submit(), cancellationToken);
    public Task StartProcessingAsync(Guid id, string actorReference, CancellationToken cancellationToken) => TransitionAsync(id, actorReference, application => application.StartProcessing(), cancellationToken);
    public Task ApproveAsync(Guid id, string actorReference, CancellationToken cancellationToken) => TransitionAsync(id, actorReference, application => application.Approve(), cancellationToken);
    public Task RefuseAsync(Guid id, string reason, string actorReference, CancellationToken cancellationToken) => TransitionAsync(id, actorReference, application => application.Refuse(reason), cancellationToken);
    public Task CancelAsync(Guid id, string actorReference, CancellationToken cancellationToken) => TransitionAsync(id, actorReference, application => application.Cancel(), cancellationToken);

    public Task<IReadOnlyList<ApplicationLifecycleAudit>> GetLifecycleAsync(Guid id, CancellationToken cancellationToken) =>
        lifecycleAudits.GetByApplicationAsync(id, cancellationToken);

    private async Task TransitionAsync(Guid id, string actorReference, Action<Misha.Domain.Applications.Application> transition, CancellationToken cancellationToken)
    {
        var application = await GetRequiredAsync(id, cancellationToken);
        var fromStatus = application.Status;
        transition(application);
        await lifecycleAudits.AddAsync(ApplicationLifecycleAudit.Create(application.Id, fromStatus, application.Status, actorReference), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Misha.Domain.Applications.Application> GetRequiredAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException($"Application '{id}' was not found.");
}
