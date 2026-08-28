using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace Misha.Domain.Tests.ContractTests;

public sealed class ApiContractTests : IClassFixture<ApiContractFactory>
{
    private readonly HttpClient _client;

    public ApiContractTests(ApiContractFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Live_health_endpoint_returns_success_and_healthy_payload()
    {
        using var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_health_endpoint_returns_success_when_database_is_available()
    {
        using var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Protected_application_create_endpoint_rejects_unauthenticated_request()
    {
        using var response = await _client.PostAsJsonAsync("/applications", new { ApplicantReference = "contract-test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class ApiContractFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("misha_contract_tests")
        .WithUsername("misha")
        .WithPassword("misha_contract_password")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Misha", _postgres.GetConnectionString());
        builder.UseSetting("Authentication:Authority", "https://localhost:5001/");
        builder.UseSetting("Authentication:Audience", "contract-tests");
        builder.UseSetting("Authentication:ApiIdentifier", "https://misha-api");
    }
}
