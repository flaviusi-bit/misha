using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Application.Policy;
using Misha.Application.Watchlists;
using ApplicationEntity = Misha.Domain.Applications.Application;
using Misha.Domain.Applicants;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PolicyServiceSecurityTests
{
    [Fact]
    public async Task Passport_provider_exception_details_are_not_exposed_by_policy_evaluation()
    {
        var application = ApplicationEntity.Create("APP-123");
        application.Submit();
        application.StartProcessing();

        var passport = PassportDocument.Create(
            application.Id,
            "AB123456",
            "RO",
            "Popescu",
            "Ion",
            new DateOnly(1985, 4, 12),
            "RO",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        var engine = new CapturingPolicyEngine();
        var provider = new ThrowingPassportVerificationProvider(
            "secret provider token and internal database connection details");
        var service = new PolicyService(
            new StubApplicationRepository(application),
            new StubPassportRepository(passport),
            provider,
            new StubWatchlistCheckRepository(),
            engine);

        var result = await service.EvaluateAsync(application.Id, CancellationToken.None);

        Assert.Equal(PolicyDecision.NotReady, result.Decision);
        Assert.Equal(PassportVerificationDecision.Error, engine.Context?.PassportVerificationDecision);
        Assert.DoesNotContain("secret provider token", provider.Message, StringComparison.Ordinal);
    }

    private sealed class CapturingPolicyEngine : IPolicyEngine
    {
        public PolicyContext? Context { get; private set; }

        public PolicyEvaluation Evaluate(PolicyContext context)
        {
            Context = context;
            return new PolicyEvaluation(PolicyDecision.NotReady, ["Passport verification failed."]);
        }
    }

    private sealed class ThrowingPassportVerificationProvider(string message) : IPassportVerificationProvider
    {
        public string Name => "throwing-stub";
        public string Message { get; } = message;

        public Task<PassportVerificationResult> VerifyAsync(
            PassportDocument passport,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Message);
    }

    private sealed class StubApplicationRepository(ApplicationEntity application) : IApplicationRepository
    {
        public Task<ApplicationEntity?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationEntity?>(application.Id == id ? application : null);

        public Task<Applicant> GetOrCreateApplicantAsync(string externalReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationEntity> AddOrGetExistingAsync(ApplicationEntity application, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubPassportRepository(PassportDocument passport) : IPassportRepository
    {
        public Task<PassportDocument?> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken) =>
            Task.FromResult<PassportDocument?>(passport.ApplicationId == applicationId ? passport : null);

        public Task AddAsync(PassportDocument passport, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubWatchlistCheckRepository : IWatchlistCheckRepository
    {
        public Task<WatchlistCheck?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
            Task.FromResult<WatchlistCheck?>(null);

        public Task AddAsync(WatchlistCheck check, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
