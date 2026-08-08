using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Infrastructure.Watchlists;

public sealed class HttpWatchlistProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IWatchlistProvider
{
    private const string ClientName = "watchlist";

    public string Name => configuration["Watchlist:ProviderName"]?.Trim() is { Length: > 0 } name
        ? name
        : "configured-http";

    public async Task<WatchlistProviderResult> CheckAsync(
        PassportDocument passport,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Watchlist:BaseUrl"]?.Trim();
        var endpoint = configuration["Watchlist:Endpoint"]?.Trim() ?? "/screen";
        var apiKey = configuration["Watchlist:ApiKey"]?.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return new WatchlistProviderResult(
                WatchlistDecision.Error,
                ErrorMessage: "Watchlist provider is not configured with an HTTPS BaseUrl.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new WatchlistProviderResult(
                WatchlistDecision.Error,
                ErrorMessage: "Watchlist provider API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, endpoint))
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

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new WatchlistProviderResult(
                    WatchlistDecision.Error,
                    ErrorMessage: $"Watchlist provider returned HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<WatchlistResponse>(cancellationToken);
            if (result is null)
            {
                return new WatchlistProviderResult(
                    WatchlistDecision.Error,
                    ErrorMessage: "Watchlist provider returned an empty response.");
            }

            if (!Enum.TryParse<WatchlistDecision>(result.Decision, ignoreCase: true, out var decision) ||
                decision is WatchlistDecision.NotChecked or WatchlistDecision.Error)
            {
                return new WatchlistProviderResult(
                    WatchlistDecision.Error,
                    ErrorMessage: "Watchlist provider returned an invalid decision.");
            }

            return new WatchlistProviderResult(decision, result.MatchReference, result.ErrorMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new WatchlistProviderResult(
                WatchlistDecision.Error,
                ErrorMessage: "Watchlist provider request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new WatchlistProviderResult(
                WatchlistDecision.Error,
                ErrorMessage: $"Watchlist provider request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new WatchlistProviderResult(
                WatchlistDecision.Error,
                ErrorMessage: $"Watchlist provider returned invalid JSON: {ex.Message}");
        }
    }

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
