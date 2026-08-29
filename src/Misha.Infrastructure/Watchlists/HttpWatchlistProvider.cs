using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Misha.Infrastructure.Watchlists;

public sealed class HttpWatchlistProvider : IWatchlistProvider
{
    private const string ClientName = "watchlist";
    private static readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> Pipelines = new(StringComparer.OrdinalIgnoreCase);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _configurationSection;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    public HttpWatchlistProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : this(httpClientFactory, configuration, "Watchlist")
    {
    }

    public HttpWatchlistProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        string configurationSection)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _configurationSection = configurationSection;
        _resiliencePipeline = Pipelines.GetOrAdd(configurationSection, _ => BuildResiliencePipeline());
    }

    public string Name => Get("ProviderName")?.Trim() is { Length: > 0 } name
        ? name
        : Get("Name")?.Trim() is { Length: > 0 } configuredName
            ? configuredName
            : "configured-http";

    public async Task<WatchlistProviderResult> CheckAsync(
        PassportDocument passport,
        CancellationToken cancellationToken)
    {
        var baseUrl = Get("BaseUrl")?.Trim();
        var endpoint = Get("Endpoint")?.Trim() ?? "/screen";
        var apiKey = Get("ApiKey")?.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            return Error("Watchlist provider is not configured with an HTTPS BaseUrl.");

        if (!Uri.TryCreate(endpoint, UriKind.Relative, out var relativeEndpoint))
            return Error("Watchlist provider Endpoint must be a relative path.");

        if (string.IsNullOrWhiteSpace(apiKey))
            return Error("Watchlist provider API key is not configured.");

        try
        {
            var client = _httpClientFactory.CreateClient(ClientName);
            using var response = await _resiliencePipeline.ExecuteAsync(
                async ct =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativeEndpoint))
                    {
                        Content = JsonContent.Create(new WatchlistRequest(
                            passport.DocumentNumber,
                            passport.IssuingCountry,
                            passport.Surname,
                            passport.GivenNames,
                            passport.DateOfBirth,
                            passport.Nationality,
                            passport.ExpiryDate))
                    };
                    request.Headers.Add("X-API-Key", apiKey);
                    return await client.SendAsync(request, ct);
                }, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Error($"Watchlist provider returned HTTP {(int)response.StatusCode}.");

            var result = await response.Content.ReadFromJsonAsync<WatchlistResponse>(cancellationToken);
            if (result is null)
                return Error("Watchlist provider returned an empty response.");

            if (!Enum.TryParse<WatchlistDecision>(result.Decision, ignoreCase: true, out var decision) ||
                decision is WatchlistDecision.NotChecked or WatchlistDecision.Error)
                return Error("Watchlist provider returned an invalid decision.");

            return new WatchlistProviderResult(decision, result.MatchReference, result.ErrorMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Error("Watchlist provider request timed out.");
        }
        catch (BrokenCircuitException)
        {
            return Error("Watchlist provider circuit is open.");
        }
        catch (HttpRequestException ex)
        {
            return Error($"Watchlist provider request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Error($"Watchlist provider returned invalid JSON: {ex.Message}");
        }
    }

    private string? Get(string key) => _configuration[$"{_configurationSection}:{key}"];

    private static ResiliencePipeline<HttpResponseMessage> BuildResiliencePipeline() =>
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(250),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(IsTransientResponse),
                OnRetry = static args =>
                {
                    args.Outcome.Result?.Dispose();
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(IsTransientResponse)
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();

    private static bool IsTransientResponse(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.RequestTimeout ||
        response.StatusCode == HttpStatusCode.TooManyRequests ||
        (int)response.StatusCode >= 500;

    private static WatchlistProviderResult Error(string message) =>
        new(WatchlistDecision.Error, ErrorMessage: message);

    private sealed record WatchlistRequest(
        string DocumentNumber,
        string IssuingCountry,
        string Surname,
        string GivenNames,
        DateOnly DateOfBirth,
        string Nationality,
        DateOnly ExpiryDate);

    private sealed record WatchlistResponse(
        [property: JsonPropertyName("decision")] string Decision,
        [property: JsonPropertyName("matchReference")] string? MatchReference,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
}
