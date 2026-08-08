using Misha.Application.Applications;
using Misha.Application.Payments;
using Misha.Domain.Applications;
using Misha.Domain.Etas;
using Misha.Domain.Payments;

namespace Misha.Application.Etas;

public sealed class EtaService(
    IApplicationRepository applications,
    IPaymentRepository payments,
    IEtaRepository etas,
    IConfiguration configuration)
{
    public async Task<EtaIssueResult> IssueAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        if (application.Status != ApplicationStatus.Approved)
            throw new InvalidOperationException("An eTA can only be issued for an approved application.");

        var payment = await payments.GetLatestAsync(applicationId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Paid)
            throw new InvalidOperationException("An eTA can only be issued after the application payment is paid.");

        var existing = await etas.GetByApplicationIdAsync(applicationId, cancellationToken);
        if (existing is not null)
            return new EtaIssueResult(existing, null, false);

        var validityDays = configuration.GetValue<int?>("Eta:ValidityDays") ?? 90;
        var (eta, verificationToken) = Eta.Issue(applicationId, validityDays);

        await etas.AddAsync(eta, cancellationToken);
        await etas.SaveChangesAsync(cancellationToken);

        return new EtaIssueResult(eta, verificationToken, true);
    }

    public Task<Eta?> GetAsync(Guid applicationId, CancellationToken cancellationToken) =>
        etas.GetByApplicationIdAsync(applicationId, cancellationToken);
}

public sealed record EtaIssueResult(Eta Eta, string? VerificationToken, bool Created);
