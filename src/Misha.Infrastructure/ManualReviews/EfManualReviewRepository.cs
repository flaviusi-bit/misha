using Microsoft.EntityFrameworkCore;
using Misha.Application.ManualReviews;
using Misha.Application.Tenants;
using Misha.Domain.ManualReviews;

namespace Misha.Infrastructure.ManualReviews;

public sealed class EfManualReviewRepository(
    MishaDbContext db,
    ITenantContext tenantContext) : IManualReviewRepository
{
    public async Task<ManualReview?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = db.ManualReviews.AsQueryable();

        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
            {
                return null;
            }

            query = query.Where(x => db.Applications
                .Any(a => a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId));
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ManualReview>> GetOpenAsync(CancellationToken cancellationToken)
    {
        var query = db.ManualReviews
            .Where(x => x.Status == ManualReviewStatus.Open);

        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
            {
                return [];
            }

            query = query.Where(x => db.Applications
                .Any(a => a.Id == x.ApplicationId && a.TenantId == tenantContext.TenantId));
        }

        return await query
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ManualReview?> GetOpenForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
            {
                return null;
            }

            var ownsApplication = await db.Applications
                .AnyAsync(a => a.Id == applicationId && a.TenantId == tenantContext.TenantId, cancellationToken);

            if (!ownsApplication)
            {
                return null;
            }
        }

        return await db.ManualReviews
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId && x.Status == ManualReviewStatus.Open, cancellationToken);
    }

    public async Task AddAsync(ManualReview review, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAdmin)
        {
            if (tenantContext.TenantId is null)
            {
                throw new UnauthorizedAccessException("Tenant context is required.");
            }

            var ownsApplication = await db.Applications
                .AnyAsync(a => a.Id == review.ApplicationId && a.TenantId == tenantContext.TenantId, cancellationToken);

            if (!ownsApplication)
            {
                throw new UnauthorizedAccessException("Application does not belong to the current tenant.");
            }
        }

        db.ManualReviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(ManualReview review, CancellationToken cancellationToken)
    {
        db.ManualReviews.Update(review);
        await db.SaveChangesAsync(cancellationToken);
    }
}