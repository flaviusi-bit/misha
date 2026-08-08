namespace Misha.Domain.Payments;

public sealed class Payment
{
    private Payment() { }

    private Payment(Guid id, Guid applicationId, long amountMinor, string currency)
    {
        Id = id;
        ApplicationId = applicationId;
        AmountMinor = amountMinor;
        Currency = currency;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static Payment Create(Guid applicationId, long amountMinor, string currency)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Payment amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be a three-letter ISO-style code.", nameof(currency));

        return new Payment(Guid.NewGuid(), applicationId, amountMinor, normalizedCurrency);
    }

    public void MarkRequiresAction(string provider, string? providerReference = null, string? actionUrl = null)
    {
        EnsurePending();
        Provider = RequireProvider(provider);
        ProviderReference = providerReference?.Trim();
        ActionUrl = ValidateActionUrl(actionUrl);
        Status = PaymentStatus.RequiresAction;
    }

    public void MarkPaid(string provider, string? providerReference = null)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.RequiresAction))
            throw new InvalidOperationException($"Payment in status '{Status}' cannot be marked paid.");

        Provider = RequireProvider(provider);
        ProviderReference = providerReference?.Trim();
        ActionUrl = null;
        Status = PaymentStatus.Paid;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Cancelled)
            throw new InvalidOperationException($"Payment in status '{Status}' cannot be failed.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A payment failure reason is required.", nameof(reason));

        Status = PaymentStatus.Failed;
        ActionUrl = null;
        FailureReason = reason.Trim();
    }

    public void Cancel()
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Cancelled)
            throw new InvalidOperationException($"Payment in status '{Status}' cannot be cancelled.");

        Status = PaymentStatus.Cancelled;
        ActionUrl = null;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Payment in status '{Status}' cannot require customer action.");
    }

    private static string RequireProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Payment provider is required.", nameof(provider));

        return provider.Trim();
    }

    private static string? ValidateActionUrl(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl))
            return null;

        if (!Uri.TryCreate(actionUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Payment action URL must be an absolute HTTPS URL.", nameof(actionUrl));

        return uri.ToString();
    }
}
