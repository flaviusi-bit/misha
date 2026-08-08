using Misha.Domain.Payments;

namespace Misha.Application.Payments;

public interface IPaymentProvider
{
    string Name { get; }

    Task<PaymentProviderResult> CreateAsync(
        Payment payment,
        CancellationToken cancellationToken);

    Task<PaymentProviderResult> GetStatusAsync(
        Payment payment,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Payment provider status reconciliation is not supported.");
}

public sealed record PaymentProviderResult(
    PaymentStatus Status,
    string? ProviderReference = null,
    string? ActionUrl = null,
    string? ErrorMessage = null);
