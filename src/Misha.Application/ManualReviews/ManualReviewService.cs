using Misha.Application.Applications;
using Misha.Domain.ManualReviews;

namespace Misha.Application.ManualReviews;

public sealed class ManualReviewService(
    IManualReviewRepository reviews,
    IApplicationRepository applications)
{
    public Task<IReadOnlyList<ManualReviewCase>> GetOpenAsync(CancellationToken cancellationToken) =>
        reviews.GetOpenAsync(cancellationToken);

    public async Task<ManualReviewCase> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await reviews.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Manual review case '{id}' was not found.");

    public async Task AssignAsync(Guid id, string actorReference, CancellationToken cancellationToken)
    {
        var reviewCase = await GetAsync(id, cancellationToken);
        reviewCase.Assign(actorReference);
        await reviews.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveAsync(
        Guid id,
        ManualReviewResolution resolution,
        string actorReference,
        string reason,
        CancellationToken cancellationToken)
    {
        var reviewCase = await GetAsync(id, cancellationToken);
        var application = await applications.GetAsync(reviewCase.ApplicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{reviewCase.ApplicationId}' was not found.");

        reviewCase.Resolve(resolution, actorReference, reason);

        switch (resolution)
        {
            case ManualReviewResolution.Approve:
                application.Approve();
                break;
            case ManualReviewResolution.Refuse:
                application.Refuse(reason);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resolution));
        }

        // Both aggregates are tracked by the same DbContext; persist them atomically in one save.
        await reviews.SaveChangesAsync(cancellationToken);
    }
}
