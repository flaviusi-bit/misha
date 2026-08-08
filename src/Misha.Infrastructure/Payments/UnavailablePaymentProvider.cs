using Misha.Application.Payments;
using Misha.Domain.Payments;

namespace Misha.Infrastructure.Payments;

public sealed class UnavailablePaymentProvider : IPaymentProvider
{
    public string Name => "unavailable";

    public Task<PaymentProviderResult> CreateAsync(
        Payment payment,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderResult(
            PaymentStatus.Failed,
            ErrorMessage: "Payment provider is not configured."));
}
