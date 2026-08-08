using Misha.Domain.Payments;

namespace Misha.Application.Payments;

public interface IPaymentProvider
{
    string Name { get; }

    Task<PaymentProviderResult> CreateAsync(
        Payment payment,
        CancellationToken cancellationToken);
}

public sealed record PaymentProviderResult(
    PaymentStatus Status,
    string? ProviderReference = null,
    string? ActionUrl = null,
    string? ErrorMessage = null);
