using Misha.Domain.Payments;

namespace Misha.Application.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
