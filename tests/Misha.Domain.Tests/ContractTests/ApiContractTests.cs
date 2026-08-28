using System.Net;
using System.Net.Http.Json;
using Misha.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Misha.Domain.Tests.ContractTests;

public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_endpoint_returns_success_and_json()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Readiness_endpoint_returns_success_and_json()
    {
        using var response = await _client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Protected_write_endpoint_rejects_unauthenticated_request()
    {
        using var response = await _client.PostAsJsonAsync("/applications", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
