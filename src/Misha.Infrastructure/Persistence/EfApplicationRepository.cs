using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Domain.Applicants;
using DomainApplication = Misha.Domain.Applications.Application;
namespace Misha.Infrastructure.Persistence;
public sealed class EfApplicationRepository(MishaDbContext db) : IApplicationRepository
{
    public Task<Applicant> GetOrCreateApplicantAsync(string externalReference,CancellationToken cancellationToken)=>GetOrCreateApplicantAsync(externalReference,"legacy",cancellationToken);
    public async Task<Applicant> GetOrCreateApplicantAsync(string externalReference,string tenantId,CancellationToken cancellationToken){var normalizedReference=externalReference.Trim();var normalizedTenant=tenantId.Trim();var existing=await db.Applicants.SingleOrDefaultAsync(x=>x.ExternalReference==normalizedReference&&x.TenantId==normalizedTenant,cancellationToken);if(existing is not null)return existing;var applicant=Applicant.Create(normalizedReference,normalizedTenant);db.Applicants.Add(applicant);try{await db.SaveChangesAsync(cancellationToken);return applicant;}catch(DbUpdateException){var concurrent=await db.Applicants.SingleOrDefaultAsync(x=>x.ExternalReference==normalizedReference&&x.TenantId==normalizedTenant,cancellationToken);if(concurrent is not null)return concurrent;throw;}}
    public Task<DomainApplication?> GetAsync(Guid id,CancellationToken cancellationToken)=>db.Applications.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);
    public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey,CancellationToken cancellationToken)=>db.Applications.SingleOrDefaultAsync(x=>x.IdempotencyKey==idempotencyKey,cancellationToken);
    public async Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application,CancellationToken cancellationToken){db.Applications.Add(application);try{await db.SaveChangesAsync(cancellationToken);return application;}catch(DbUpdateException)when(!string.IsNullOrWhiteSpace(application.IdempotencyKey)){var existing=await db.Applications.SingleOrDefaultAsync(x=>x.IdempotencyKey==application.IdempotencyKey,cancellationToken);if(existing is not null)return existing;throw;}}
    public Task SaveChangesAsync(CancellationToken cancellationToken)=>db.SaveChangesAsync(cancellationToken);
}
