namespace Misha.Domain.Watchlists;

public sealed class WatchlistCheck
{
    private WatchlistCheck() { }

    private WatchlistCheck(Guid id, Guid applicationId, string provider)
    {
        Id = id;
        ApplicationId = applicationId;
        Provider = provider;
        Decision = WatchlistDecision.NotChecked;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public WatchlistDecision Decision { get; private set; }
    public string? MatchReference { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CheckedAtUtc { get; private set; }

    public static WatchlistCheck Create(Guid applicationId, string provider)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Watchlist provider is required.", nameof(provider));

        return new WatchlistCheck(Guid.NewGuid(), applicationId, provider.Trim());
    }

    public void Complete(WatchlistDecision decision, string? matchReference = null)
    {
        if (decision is WatchlistDecision.NotChecked or WatchlistDecision.Error)
            throw new ArgumentException("A completed watchlist check requires a valid decision.", nameof(decision));

        Decision = decision;
        MatchReference = string.IsNullOrWhiteSpace(matchReference) ? null : matchReference.Trim();
        ErrorMessage = null;
        CheckedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message is required.", nameof(message));

        Decision = WatchlistDecision.Error;
        ErrorMessage = message.Trim();
        MatchReference = null;
        CheckedAtUtc = DateTimeOffset.UtcNow;
    }
}
