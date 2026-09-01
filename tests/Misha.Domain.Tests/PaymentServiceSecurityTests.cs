using Misha.Application.Applications;
using Misha.Application.Payments;
using Misha.Domain.Applicants;
using Misha.Domain.Payments;
using DomainApplication = Misha.Domain.Applications.Application;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PaymentServiceSecurityTests
{
    [Fact]
    public async Task Create_does_not_persist_provider_error_message()
    {
        var application = CreateSubmittedApplication();
        var payments = new FakePaymentRepository();
        var provider = new FakePaymentProvider(
            new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: "provider-secret: internal gateway credentials and upstream stack trace"));
        var service = new PaymentService(
            new FakeApplicationRepository(application),
            payments,
            provider);

        var payment = await service.CreateAsync(
            application.Id,
            1000,
            "EUR",
            CancellationToken.None);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Payment could not be completed.", payment.FailureReason);
        Assert.DoesNotContain("provider-secret", payment.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_converts_unexpected_provider_exception_to_safe_failure()
    {
        var application = CreateSubmittedApplication();
        var payments = new FakePaymentRepository();
        var provider = new ThrowingPaymentProvider(
            new InvalidOperationException("secret upstream exception details"));
        var service = new PaymentService(
            new FakeApplicationRepository(application),
            payments,
            provider);

        var payment = await service.CreateAsync(
            application.Id,
            1000,
            "EUR",
            CancellationToken.None);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Payment could not be completed.", payment.FailureReason);
    }

    private static DomainApplication CreateSubmittedApplication()
    {
        var application = DomainApplication.Create("applicant-123");
        application.Submit();
        return application;
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public Task<Payment?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
            Task.FromResult<Payment?>(null);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePaymentProvider(PaymentProviderResult result) : IPaymentProvider
    {
        public string Name => "test-provider";

        public Task<PaymentProviderResult> CreateAsync(Payment payment, CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<PaymentProviderResult> GetStatusAsync(Payment payment, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class ThrowingPaymentProvider(Exception exception) : IPaymentProvider
    {
        public string Name => "test-provider";

        public Task<PaymentProviderResult> CreateAsync(Payment payment, CancellationToken cancellationToken) =>
            Task.FromException<PaymentProviderResult>(exception);

        public Task<PaymentProviderResult> GetStatusAsync(Payment payment, CancellationToken cancellationToken) =>
            Task.FromException<PaymentProviderResult>(exception);
    }

    private sealed class FakeApplicationRepository(DomainApplication application) : IApplicationRepository
    {
        public Task<Applicant> GetOrCreateApplicantAsync(string externalReference, CancellationToken cancellationToken) =>
            Task.FromResult(Applicant.Create(externalReference));

        public Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DomainApplication?>(id == application.Id ? application : null);

        public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<DomainApplication?>(null);

        public Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken) =>
            Task.FromResult(application);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
