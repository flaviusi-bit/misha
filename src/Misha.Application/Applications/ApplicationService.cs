using System.Text.Json;
using Misha.Application.Messaging;
using Misha.Application.Tenants;
using Misha.Domain.Applications;

namespace Misha.Application.Applications;

public sealed class ApplicationService(IApplicationRepository repository,IApplicationLifecycleAuditRepository lifecycleAudits,IOutboxWriter outbox,ITenantContext tenantContext)
{
    public async Task<Guid> CreateAsync(string applicantReference,string? idempotencyKey,CancellationToken cancellationToken)
    {
        var tenantId=tenantContext.TenantId??throw new InvalidOperationException("A tenant context is required.");
        if(!string.IsNullOrWhiteSpace(idempotencyKey)){var existing=await repository.GetByIdempotencyKeyAsync(idempotencyKey.Trim(),cancellationToken);if(existing is not null){if(!string.Equals(existing.ApplicantReference,applicantReference.Trim(),StringComparison.Ordinal))throw new InvalidOperationException("The idempotency key has already been used for a different application request.");return existing.Id;}}
        var applicant=await repository.GetOrCreateApplicantAsync(applicantReference,cancellationToken);
        var application=Misha.Domain.Applications.Application.Create(tenantId,applicant.Id,applicant.ExternalReference,idempotencyKey);
        var persisted=await repository.AddOrGetExistingAsync(application,cancellationToken);
        if(!string.Equals(persisted.ApplicantReference,applicantReference.Trim(),StringComparison.Ordinal))throw new InvalidOperationException("The idempotency key has already been used for a different application request.");
        return persisted.Id;
    }
    public Task<Guid> CreateAsync(string applicantReference,CancellationToken cancellationToken)=>CreateAsync(applicantReference,null,cancellationToken);
    public Task<Misha.Domain.Applications.Application?> GetAsync(Guid id,CancellationToken cancellationToken)=>repository.GetAsync(id,cancellationToken);
    public Task SubmitAsync(Guid id,string actorReference,CancellationToken cancellationToken)=>TransitionAsync(id,actorReference,null,a=>a.Submit(),cancellationToken);
    public Task StartProcessingAsync(Guid id,string actorReference,CancellationToken cancellationToken)=>TransitionAsync(id,actorReference,null,a=>a.StartProcessing(),cancellationToken);
    public Task ApproveAsync(Guid id,string actorReference,CancellationToken cancellationToken)=>TransitionAsync(id,actorReference,null,a=>a.Approve(),cancellationToken);
    public Task RefuseAsync(Guid id,string reason,string actorReference,CancellationToken cancellationToken)=>TransitionAsync(id,actorReference,reason,a=>a.Refuse(reason),cancellationToken);
    public Task CancelAsync(Guid id,string actorReference,CancellationToken cancellationToken)=>TransitionAsync(id,actorReference,null,a=>a.Cancel(),cancellationToken);
    public Task<IReadOnlyList<ApplicationLifecycleAudit>> GetLifecycleAsync(Guid id,CancellationToken cancellationToken)=>lifecycleAudits.GetByApplicationAsync(id,cancellationToken);
    private async Task TransitionAsync(Guid id,string actorReference,string? reason,Action<Misha.Domain.Applications.Application> transition,CancellationToken cancellationToken){var application=await GetRequiredAsync(id,cancellationToken);var fromStatus=application.Status;transition(application);var audit=ApplicationLifecycleAudit.Create(application.Id,fromStatus,application.Status,actorReference,reason);await lifecycleAudits.AddAsync(audit,cancellationToken);var eventId=Guid.NewGuid();var occurredAtUtc=audit.OccurredAtUtc;var lifecycleEvent=new ApplicationLifecycleChanged(eventId,application.Id,fromStatus.ToString(),application.Status.ToString(),reason,actorReference,occurredAtUtc);await outbox.AddAsync(eventId,"application.lifecycle.changed.v1",application.Id,JsonSerializer.Serialize(lifecycleEvent),occurredAtUtc,cancellationToken);await repository.SaveChangesAsync(cancellationToken);}
    private async Task<Misha.Domain.Applications.Application> GetRequiredAsync(Guid id,CancellationToken cancellationToken)=>await repository.GetAsync(id,cancellationToken)??throw new KeyNotFoundException($"Application '{id}' was not found.");
}
