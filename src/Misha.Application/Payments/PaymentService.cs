using Misha.Application.Applications;
using Misha.Domain.Applications;
using Misha.Domain.Payments;

namespace Misha.Application.Payments;

public sealed class PaymentService(
    IApplicationRepository applications,
    IPaymentRepository payments,
    IPaymentProvider provider)
{
    public async Task<Payment> CreateAsync(
        Guid applicationId,
        long amountMinor,
        string currency,
        CancellationToken cancellationToken)
    {
        var application = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        // ETA fees are normally collected before the final eligibility decision.
        // Payment therefore belongs after submission and before approval/refusal.
        if (application.Status is not (ApplicationStatus.Submitted or ApplicationStatus.Processing))
            throw new InvalidOperationException(
                "Payment can only be initiated for a submitted or processing application.");

        var existing = await payments.GetLatestAsync(applicationId, cancellationToken);
        if (existing is not null && existing.Status is PaymentStatus.Pending or PaymentStatus.RequiresAction)
            return existing;

        var payment = Payment.Create(applicationId, amountMinor, currency);
        await payments.AddAsync(payment, cancellationToken);

        try
        {
            var result = await provider.CreateAsync(payment, cancellationToken);
            switch (result.Status)
            {
                case PaymentStatus.Paid:
                    payment.MarkPaid(provider.Name, result.ProviderReference);
                    break;
                case PaymentStatus.RequiresAction:
                    payment.MarkRequiresAction(provider.Name, result.ProviderReference, result.ActionUrl);
                    break;
                case PaymentStatus.Failed:
                    payment.MarkFailed(result.ErrorMessage ?? "Payment provider returned a failed status.");
                    break;
                case PaymentStatus.Pending:
                    break;
                default:
                    payment.MarkFailed(result.ErrorMessage ?? "Payment provider returned an invalid status.");
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            payment.MarkFailed(ex.Message);
        }

        await payments.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public Task<Payment?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
        payments.GetLatestAsync(applicationId, cancellationToken);
}
