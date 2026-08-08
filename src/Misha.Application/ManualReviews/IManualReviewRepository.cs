using Misha.Domain.ManualReviews;

namespace Misha.Application.ManualReviews;

public interface IManualReviewRepository
{
    Task<ManualReviewCase?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ManualReviewCase>> GetOpenAsync(CancellationToken cancellationToken);
    Task<ManualReviewCase?> GetOpenForApplicationAsync(Guid applicationId, CancellationToken cancellationToken);
    Task AddAsync(ManualReviewCase reviewCase, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
