using Microsoft.EntityFrameworkCore;
using Misha.Application.ManualReviews;
using Misha.Domain.ManualReviews;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.ManualReviews;

public sealed class EfManualReviewRepository(MishaDbContext db) : IManualReviewRepository
{
    public Task<ManualReviewCase?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.ManualReviewCases.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ManualReviewCase>> GetOpenAsync(CancellationToken cancellationToken) =>
        await db.ManualReviewCases
            .AsNoTracking()
            .Where(x => x.Status == ManualReviewStatus.Pending || x.Status == ManualReviewStatus.InProgress)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ManualReviewCase?> GetOpenForApplicationAsync(Guid applicationId, CancellationToken cancellationToken) =>
        db.ManualReviewCases.SingleOrDefaultAsync(
            x => x.ApplicationId == applicationId &&
                 (x.Status == ManualReviewStatus.Pending || x.Status == ManualReviewStatus.InProgress),
            cancellationToken);

    public Task AddAsync(ManualReviewCase reviewCase, CancellationToken cancellationToken)
    {
        db.ManualReviewCases.Add(reviewCase);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
