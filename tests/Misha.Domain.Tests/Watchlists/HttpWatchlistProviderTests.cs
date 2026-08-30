using System.Net;
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
        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Clear, result.Decision);
        Assert.Single(handler.Requests);
        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://watchlist.example.test/screen", request.RequestUri?.ToString());
        Assert.Equal("test-api-key", Assert.Single(request.Headers.GetValues("X-API-Key")));
        var body = Assert.Single(handler.RequestBodies);
        Assert.Contains("\"documentNumber\":\"AB123456\"", body);
        Assert.Contains("\"issuingCountry\":\"ROU\"", body);
        Assert.Contains("\"surname\":\"DOE\"", body);
        Assert.Contains("\"givenNames\":\"JOHN\"", body);
        Assert.Contains("\"nationality\":\"ROU\"", body);
    }

    [Fact]
    public async Task Supports_named_provider_configuration_section()
    {
        var handler = new RecordingHandler(_ => Json("{\"decision\":\"Clear\",\"matchReference\":null,\"errorMessage\":null}"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Watchlist:Providers:ProviderA:Name"] = "provider-a",
            ["Watchlist:Providers:ProviderA:BaseUrl"] = "https://provider-a.example.test",
            ["Watchlist:Providers:ProviderA:Endpoint"] = "/screen",
            ["Watchlist:Providers:ProviderA:ApiKey"] = "provider-a-key"
        }).Build();
        var provider = new HttpWatchlistProvider(new TestHttpClientFactory(handler), configuration, "Watchlist:Providers:ProviderA");

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal("provider-a", provider.Name);
        Assert.Equal(WatchlistDecision.Clear, result.Decision);
        Assert.Equal("https://provider-a.example.test/screen", handler.Requests.Single().RequestUri?.ToString());
        Assert.Equal("provider-a-key", Assert.Single(handler.Requests.Single().Headers.GetValues("X-API-Key")));
    }

    [Fact]
    public async Task Maps_potential_and_confirmed_matches()
    {
        var handler = new RecordingHandler(request =>
            handler.Requests.Count == 0
                ? Json("{\"decision\":\"PotentialMatch\",\"matchReference\":\"CASE-42\",\"errorMessage\":null}")
                : Json("{\"decision\":\"ConfirmedMatch\",\"matchReference\":\"CASE-43\",\"errorMessage\":null}"));
        var provider = CreateProvider(handler);

        var potential = await provider.CheckAsync(CreatePassport(), CancellationToken.None);
        var confirmed = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.PotentialMatch, potential.Decision);
        Assert.Equal("CASE-42", potential.MatchReference);
        Assert.Equal(WatchlistDecision.ConfirmedMatch, confirmed.Decision);
        Assert.Equal("CASE-43", confirmed.MatchReference);
    }

    [Fact]
    public async Task Treats_http_500_as_provider_error_after_transient_retries()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider returned HTTP 500.", result.ErrorMessage);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Rejects_invalid_provider_decision()
    {
        var handler = new RecordingHandler(_ => Json("{\"decision\":\"UnknownDecision\",\"matchReference\":null,\"errorMessage\":null}"));
        var result = await CreateProvider(handler).CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider returned an invalid decision.", result.ErrorMessage);
    }

    [Fact]
    public async Task Empty_response_is_provider_error()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var result = await CreateProvider(handler).CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider returned an empty response.", result.ErrorMessage);
    }

    [Fact]
    public async Task Invalid_json_is_provider_error_without_payload_details()
    {
        var handler = new RecordingHandler(_ => Json("{not-json"));
        var result = await CreateProvider(handler).CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider returned invalid JSON.", result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_http_base_url_without_network_call()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("network call must not happen"));
        var provider = CreateProvider(handler, baseUrl: "http://watchlist.example.test");

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider is not configured with an HTTPS BaseUrl.", result.ErrorMessage);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejects_absolute_endpoint_without_network_call()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("network call must not happen"));
        var provider = CreateProvider(handler, endpoint: "https://evil.example.test/screen");

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider Endpoint must be a relative path.", result.ErrorMessage);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejects_missing_api_key_without_network_call()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("network call must not happen"));
        var provider = CreateProvider(handler, apiKey: " ");

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider API key is not configured.", result.ErrorMessage);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Does_not_expose_api_key_when_http_request_fails()
    {
        const string secret = "super-secret-api-key";
        var handler = new RecordingHandler(_ => throw new HttpRequestException($"connection failed using {secret}"));
        var provider = CreateProvider(handler, apiKey: secret);

        var result = await provider.CheckAsync(CreatePassport(), CancellationToken.None);

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.Equal("Watchlist provider request failed.", result.ErrorMessage);
        Assert.DoesNotContain(secret, result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json("{\"decision\":\"Clear\"}");
        });
        var provider = CreateProvider(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.CheckAsync(CreatePassport(), cts.Token));
    }

    private static HttpWatchlistProvider CreateProvider(RecordingHandler handler, string baseUrl = "https://watchlist.example.test", string endpoint = "/screen", string apiKey = "test-api-key")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Watchlist:BaseUrl"] = baseUrl,
            ["Watchlist:Endpoint"] = endpoint,
            ["Watchlist:ApiKey"] = apiKey,
            ["Watchlist:ProviderName"] = "contract-test-provider"
        }).Build();
        return new HttpWatchlistProvider(new TestHttpClientFactory(handler), configuration);
    }

    private static PassportDocument CreatePassport() => PassportDocument.Create(Guid.NewGuid(), "AB123456", "ROU", "DOE", "JOHN", new DateOnly(1990, 1, 1), "ROU", new DateOnly(2030, 1, 1));

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK) { Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json") };

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            : this((request, _) => Task.FromResult(responder(request))) { }

        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) => _responder = responder;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return await _responder(request, cancellationToken);
        }
    }
}
