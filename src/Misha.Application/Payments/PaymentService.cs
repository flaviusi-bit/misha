using Misha.Application.Applications;
using Misha.Domain.Applications;
using Misha.Domain.Payments;

namespace Misha.Application.Payments;

public sealed class PaymentService(
    IApplicationRepository applications,
    IPaymentRepository payments,
    IPaymentProvider provider)
{
    private const string GenericProviderFailure = "Payment could not be completed.";

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
            ApplyProviderResult(payment, result);
        }
        catch (Exception) when (true)
        {
            // Provider implementation details must never cross the application/API boundary.
            // Preserve cancellation semantics while converting unexpected provider failures to a safe result.
            if (cancellationToken.IsCancellationRequested)
                throw;

            payment.MarkFailed(GenericProviderFailure);
        }

        await payments.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> ReconcileAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var payment = await payments.GetLatestAsync(applicationId, cancellationToken);
        if (payment is null || payment.Status is PaymentStatus.Paid or PaymentStatus.Failed or PaymentStatus.Cancelled)
            return payment;

        if (string.IsNullOrWhiteSpace(payment.ProviderReference))
            return payment;

        PaymentProviderResult result;
        try
        {
            result = await provider.GetStatusAsync(payment, cancellationToken);
        }
        catch (NotSupportedException)
        {
            return payment;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return payment;
        }

        ApplyProviderResult(payment, result);
        await payments.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public Task<Payment?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
        payments.GetLatestAsync(applicationId, cancellationToken);

    private void ApplyProviderResult(Payment payment, PaymentProviderResult result)
    {
        switch (result.Status)
        {
            case PaymentStatus.Paid:
                payment.MarkPaid(provider.Name, result.ProviderReference ?? payment.ProviderReference);
                break;
            case PaymentStatus.RequiresAction:
                payment.MarkRequiresAction(
                    provider.Name,
                    result.ProviderReference ?? payment.ProviderReference,
                    result.ActionUrl ?? payment.ActionUrl);
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed(GenericProviderFailure);
                break;
            case PaymentStatus.Pending:
                break;
            default:
                payment.MarkFailed(GenericProviderFailure);
                break;
        }
    }
}
