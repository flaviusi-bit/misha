using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Application.Watchlists;
using Misha.Domain.Applications;
using Misha.Domain.Watchlists;

namespace Misha.Application.Policy;

public sealed class PolicyService(
    IApplicationRepository applications,
    IPassportRepository passports,
    IPassportVerificationProvider passportVerification,
    IWatchlistCheckRepository watchlists,
    IPolicyEngine engine,
    IRiskAssessmentEngine? riskAssessmentEngine = null)
{
    private readonly IRiskAssessmentEngine _riskAssessmentEngine = riskAssessmentEngine ?? new DeterministicRiskAssessmentEngine();

    public async Task<PolicyEvaluation> EvaluateAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        var passport = await passports.GetByApplicationAsync(applicationId, cancellationToken);
        var watchlist = await watchlists.GetLatestAsync(applicationId, cancellationToken);

        if (passport is null)
        {
            return engine.Evaluate(new PolicyContext(
                application.Status,
                HasPassport: false,
                PassportExpired: false,
                PassportVerificationDecision.NotVerified,
                watchlist?.Decision ?? WatchlistDecision.NotChecked));
        }

        PassportVerificationResult verification;
        try
        {
            verification = await passportVerification.VerifyAsync(passport, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            verification = new PassportVerificationResult(
                PassportVerificationDecision.Error,
                ErrorMessage: ex.Message);
        }

        return engine.Evaluate(new PolicyContext(
            application.Status,
            HasPassport: true,
            PassportExpired: passport.IsExpired(DateOnly.FromDateTime(DateTime.UtcNow)),
            verification.Decision,
            watchlist?.Decision ?? WatchlistDecision.NotChecked));
    }

    public async Task<RiskAssessment> AssessRiskAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var policyEvaluation = await EvaluateAsync(applicationId, cancellationToken);
        return _riskAssessmentEngine.Assess(policyEvaluation);
    }
}
