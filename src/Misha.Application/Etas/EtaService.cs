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
    int validityDays)
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

        var (eta, verificationToken) = Eta.Issue(applicationId, validityDays);

        await etas.AddAsync(eta, cancellationToken);
        await etas.SaveChangesAsync(cancellationToken);

        return new EtaIssueResult(eta, verificationToken, true);
    }

    public Task<Eta?> GetAsync(Guid applicationId, CancellationToken cancellationToken) =>
        etas.GetByApplicationIdAsync(applicationId, cancellationToken);

    public async Task<EtaVerificationResult?> VerifyAsync(
        string etaNumber,
        string verificationToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(etaNumber) || string.IsNullOrWhiteSpace(verificationToken))
            return null;

        var hash = Eta.HashVerificationToken(verificationToken);
        var eta = await etas.GetByVerificationTokenHashAsync(hash, cancellationToken);

        if (eta is null || !string.Equals(eta.EtaNumber, etaNumber.Trim(), StringComparison.Ordinal))
            return null;

        var now = DateTimeOffset.UtcNow;
        var status = eta.Status switch
        {
            EtaStatus.Revoked => EtaVerificationStatus.Revoked,
            _ when now >= eta.ExpiresAtUtc => EtaVerificationStatus.Expired,
            _ => EtaVerificationStatus.Valid
        };

        return new EtaVerificationResult(
            eta.EtaNumber,
            status,
            eta.IssuedAtUtc,
            eta.ExpiresAtUtc,
            eta.RevokedAtUtc);
    }

    public async Task RevokeAsync(
        Guid applicationId,
        string reason,
        CancellationToken cancellationToken)
    {
        var eta = await etas.GetByApplicationIdAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETA for application '{applicationId}' was not found.");

        eta.Revoke(reason);
        await etas.SaveChangesAsync(cancellationToken);
    }
}

public sealed record EtaIssueResult(Eta Eta, string? VerificationToken, bool Created);

public enum EtaVerificationStatus
{
    Valid,
    Expired,
    Revoked
}

public sealed record EtaVerificationResult(
    string EtaNumber,
    EtaVerificationStatus Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc);
