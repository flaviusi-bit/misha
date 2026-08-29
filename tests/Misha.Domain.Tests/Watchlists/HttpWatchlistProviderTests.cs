using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;
using Misha.Infrastructure.Watchlists;
using Xunit;

namespace Misha.Domain.Tests.Watchlists;

public sealed class HttpWatchlistProviderTests
{
    [Fact]
    public async Task Sends_expected_screening_contract_and_maps_clear_response()
    {
        var handler = new RecordingHandler(_ => Json("{\"decision\":\"Clear\",\"matchReference\":null,\"errorMessage\":null}"));
        var provider = CreateProvider(handler);
        var passport = CreatePassport();

        var result = await provider.CheckAsync(passport, CancellationToken.None);

        Assert.Equal(WatchlistDecision.Clear, result.Decision);
        Assert.Single(handler.Requests);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://watchlist.example.test/screen", request.RequestUri?.ToString());
        Assert.True(request.Headers.TryGetValues("X-API-Key", out var apiKeys));
        Assert.Equal("test-api-key", Assert.Single(apiKeys));

        var body = Assert.Single(handler.RequestBodies);
        Assert.Contains("\"documentNumber\":\"AB123456\"", body);
        Assert.Contains("\"issuingCountry\":\"ROU\"", body);
        Assert.Contains("\"surname\":\"DOE\"", body);
        Assert.Contains("\"givenNames\":\"JOHN\"", body);
        Assert.Contains("\"nationality\":\"ROU\"", body);
    }

    [Fact]
    public async Task Maps_potential_match_and_match_reference()
    {
        var handler = new RecordingHandler(_ => Json("{\"decision\":\"PotentialMatch\",\"matchReference\":\"CASE-42\",\"errorMessage\":null}"));
        var provider = CreateProvider(handler);

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.PotentialMatch, result.Decision);
        Assert.Equal("CASE-42", result.MatchReference);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Treats_http_500_as_provider_error_after_transient_retries()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider returned HTTP 500.", result.ErrorMessage);
        Assert.Collection(handler.Requests, _ => { }, _ => { }, _ => { });
    }

    [Fact]
    public async Task Rejects_invalid_provider_decision()
    {
        var handler = new RecordingHandler(_ => Json("{\"decision\":\"UnknownDecision\",\"matchReference\":null,\"errorMessage\":null}"));
        var provider = CreateProvider(handler);

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider returned an invalid decision.", result.ErrorMessage);
    }

    private static HttpWatchlistProvider CreateProvider(RecordingHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Watchlist:BaseUrl"] = "https://watchlist.example.test",
                ["Watchlist:Endpoint"] = "/screen",
                ["Watchlist:ApiKey"] = "test-api-key",
                ["Watchlist:ProviderName"] = "contract-test-provider"
            })
            .Build();

        var factory = new TestHttpClientFactory(handler);
        return new HttpWatchlistProvider(factory, configuration);
    }

    private static PassportDocument CreatePassport() => PassportDocument.Create(
        Guid.NewGuid(),
        "AB123456",
        "ROU",
        "DOE",
        "JOHN",
        new DateOnly(1990, 1, 1),
        "ROU",
        new DateOnly(2030, 1, 1));

    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return responder(request);
        }
    }
}
