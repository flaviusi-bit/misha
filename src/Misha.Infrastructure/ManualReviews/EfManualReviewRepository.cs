using Microsoft.EntityFrameworkCore;
using Misha.Application.ManualReviews;
using Misha.Application.Tenants;
using Misha.Domain.ManualReviews;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.ManualReviews;

public sealed class EfManualReviewRepository(
    MishaDbContext db,
    ITenantContext tenantContext) : IManualReviewRepository
{
    public Task<ManualReviewCase?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = db.ManualReviewCases.AsQueryable();

        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
                return Task.FromResult<ManualReviewCase?>(null);

            query = query.Where(x => db.Applications
                .Any(a => a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId));
        }

        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ManualReviewCase>> GetOpenAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        var query = db.ManualReviewCases
            .AsNoTracking()
            .Where(x => x.Status == ManualReviewStatus.Pending || x.Status == ManualReviewStatus.InProgress);

        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
                return [];

            query = query.Where(x => db.Applications
                .Any(a => a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId));
        }

        return await query
            .OrderBy(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ManualReviewCase?> GetOpenForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
                return null;

            var ownsApplication = await db.Applications
                .AnyAsync(a => a.Id == applicationId && a.TenantId == tenantContext.TenantId, cancellationToken);

            if (!ownsApplication)
                return null;
        }

        return await db.ManualReviewCases.SingleOrDefaultAsync(
            x => x.ApplicationId == applicationId &&
                 (x.Status == ManualReviewStatus.Pending || x.Status == ManualReviewStatus.InProgress),
            cancellationToken);
    }

    public async Task AddAsync(ManualReviewCase reviewCase, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
                throw new UnauthorizedAccessException("Tenant context is required.");

            var ownsApplication = await db.Applications
                .AnyAsync(a => a.Id == reviewCase.ApplicationId && a.TenantId == tenantContext.TenantId, cancellationToken);

            if (!ownsApplication)
                throw new UnauthorizedAccessException("Application does not belong to the current tenant.");
        }

        db.ManualReviewCases.Add(reviewCase);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
