using Misha.Application.Applications;
using Misha.Application.Payments;
using Misha.Domain.Applicants;
using Misha.Domain.Payments;
using DomainApplication = Misha.Domain.Applications.Application;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PaymentServiceReconciliationTests
{
    [Fact]
    public async Task Reconcile_moves_requires_action_payment_to_paid()
    {
        var applicationId = Guid.NewGuid();
        var payment = Payment.Create(applicationId, 1000, "EUR");
        payment.MarkRequiresAction("test-provider", "ref-123", "https://pay.example.test/continue");

        var payments = new FakePaymentRepository(payment);
        var provider = new FakePaymentProvider(new PaymentProviderResult(PaymentStatus.Paid, "ref-123"));
        var service = new PaymentService(new FakeApplicationRepository(), payments, provider);

        var result = await service.ReconcileAsync(applicationId, CancellationToken.None);

        Assert.Same(payment, result);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("ref-123", payment.ProviderReference);
        Assert.Equal(1, provider.StatusCalls);
        Assert.Equal(1, payments.SaveCalls);
    }

    [Fact]
    public async Task Reconcile_does_not_change_terminal_payment()
    {
        var applicationId = Guid.NewGuid();
        var payment = Payment.Create(applicationId, 1000, "EUR");
        payment.MarkPaid("test-provider", "ref-123");

        var payments = new FakePaymentRepository(payment);
        var provider = new FakePaymentProvider(new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: "should not be called"));
        var service = new PaymentService(new FakeApplicationRepository(), payments, provider);

        var result = await service.ReconcileAsync(applicationId, CancellationToken.None);

        Assert.Same(payment, result);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(0, provider.StatusCalls);
        Assert.Equal(0, payments.SaveCalls);
    }

    private sealed class FakePaymentRepository(Payment payment) : IPaymentRepository
    {
        public int SaveCalls { get; private set; }

        public Task<Payment?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
            Task.FromResult<Payment?>(payment.ApplicationId == applicationId ? payment : null);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentProvider(PaymentProviderResult result) : IPaymentProvider
    {
        public int StatusCalls { get; private set; }
        public string Name => "test-provider";

        public Task<PaymentProviderResult> CreateAsync(Payment payment, CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<PaymentProviderResult> GetStatusAsync(Payment payment, CancellationToken cancellationToken)
        {
            StatusCalls++;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeApplicationRepository : IApplicationRepository
    {
        public Task<Applicant> GetOrCreateApplicantAsync(string externalReference, CancellationToken cancellationToken) =>
            Task.FromResult(Applicant.Create(externalReference, "test-tenant"));

        public Task<Applicant> GetOrCreateApplicantAsync(string externalReference, string tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(Applicant.Create(externalReference, tenantId));

        public Task<DomainApplication?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DomainApplication?>(null);

        public Task<DomainApplication?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<DomainApplication?>(null);

        public Task<DomainApplication> AddOrGetExistingAsync(DomainApplication application, CancellationToken cancellationToken) =>
            Task.FromResult(application);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
