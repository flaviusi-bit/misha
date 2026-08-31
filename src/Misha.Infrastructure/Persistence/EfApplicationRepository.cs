using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Application.Tenants;
using Misha.Domain.Applicants;
using DomainApplication = Misha.Domain.Applications.Application;

namespace Misha.Infrastructure.Persistence;

public sealed class EfApplicationRepository(MishaDbContext db, ITenantContext tenantContext) : IApplicationRepository
{
    public async Task<Applicant> GetOrCreateApplicantAsync(string externalReference, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenant();
        var normalizedReference = externalReference.Trim();
        var existing = await db.Applicants.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ExternalReference == normalizedReference, cancellationToken);
        if (existing is not null) return existing;
        var applicant = Applicant.Create(tenantId, normalizedReference);
        db.Applicants.Add(applicant);
        try { await db.SaveChangesAsync(cancellationToken); return applicant; }
        catch (DbUpdateException)
        {
            var concurrent = await db.Applicants.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ExternalReference == normalizedReference, cancellationToken);
            if (concurrent is not null) return concurrent;
            throw;
        }
    }

    public Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Applications.SingleOrDefaultAsync(x => (tenantContext.IsAdmin || x.TenantId == tenantContext.TenantId) && x.Id == id, cancellationToken);

    public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        db.Applications.SingleOrDefaultAsync(x => (tenantContext.IsAdmin || x.TenantId == tenantContext.TenantId) && x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAdmin && application.TenantId != tenantContext.TenantId) throw new InvalidOperationException("Application tenant does not match the authenticated tenant.");
        db.Applications.Add(application);
        try { await db.SaveChangesAsync(cancellationToken); return application; }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(application.IdempotencyKey))
        {
            var existing = await db.Applications.SingleOrDefaultAsync(x => (tenantContext.IsAdmin || x.TenantId == tenantContext.TenantId) && x.IdempotencyKey == application.IdempotencyKey, cancellationToken);
            if (existing is not null) return existing;
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
    private Guid RequireTenant() => tenantContext.TenantId ?? throw new InvalidOperationException("A tenant context is required.");
}
