using Misha.Domain.Payments;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PaymentTests
{
    [Fact]
    public void Create_normalizes_currency_and_starts_pending()
    {
        var applicationId = Guid.NewGuid();

        var payment = Payment.Create(applicationId, 12500, "eur");

        Assert.Equal(applicationId, payment.ApplicationId);
        Assert.Equal(12500, payment.AmountMinor);
        Assert.Equal("EUR", payment.Currency);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Create_rejects_non_positive_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Payment.Create(Guid.NewGuid(), 0, "EUR"));
    }

    [Fact]
    public void MarkPaid_sets_provider_reference_and_completion_time()
    {
        var payment = Payment.Create(Guid.NewGuid(), 12500, "EUR");

        payment.MarkPaid("stripe", "pay_123");

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("stripe", payment.Provider);
        Assert.Equal("pay_123", payment.ProviderReference);
        Assert.Null(payment.ActionUrl);
        Assert.NotNull(payment.CompletedAtUtc);
    }

    [Fact]
    public void MarkRequiresAction_stores_secure_action_url()
    {
        var payment = Payment.Create(Guid.NewGuid(), 12500, "EUR");

        payment.MarkRequiresAction("provider", "pay_123", "https://payments.example/checkout/pay_123");

        Assert.Equal(PaymentStatus.RequiresAction, payment.Status);
        Assert.Equal("https://payments.example/checkout/pay_123", payment.ActionUrl);
    }

    [Fact]
    public void MarkRequiresAction_rejects_non_https_action_url()
    {
        var payment = Payment.Create(Guid.NewGuid(), 12500, "EUR");

        Assert.Throws<ArgumentException>(() =>
            payment.MarkRequiresAction("provider", "pay_123", "http://payments.example/checkout/pay_123"));
    }

    [Fact]
    public void MarkFailed_requires_a_reason()
    {
        var payment = Payment.Create(Guid.NewGuid(), 12500, "EUR");

        Assert.Throws<ArgumentException>(() => payment.MarkFailed(" "));
    }

    [Fact]
    public void Paid_payment_cannot_be_cancelled()
    {
        var payment = Payment.Create(Guid.NewGuid(), 12500, "EUR");
        payment.MarkPaid("stripe", "pay_123");

        Assert.Throws<InvalidOperationException>(() => payment.Cancel());
    }
}
