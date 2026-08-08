using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Misha.Application.Documents;
using Misha.Domain.Documents;

namespace Misha.Infrastructure.Documents;

public sealed class HttpPassportVerificationProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IPassportVerificationProvider
{
    private const string ClientName = "passport-verification";

    public string Name => configuration["PassportVerification:ProviderName"]?.Trim() is { Length: > 0 } name
        ? name
        : "configured-http";

    public async Task<PassportVerificationResult> VerifyAsync(
        PassportDocument passport,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["PassportVerification:BaseUrl"]?.Trim();
        var endpoint = configuration["PassportVerification:Endpoint"]?.Trim() ?? "/verify";
        var apiKey = configuration["PassportVerification:ApiKey"]?.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.UnableToVerify,
                ErrorMessage: "Passport verification provider is not configured with an HTTPS BaseUrl.");
        }

        if (string.IsNullOrWhiteSpace(endpoint) || !endpoint.StartsWith('/', StringComparison.Ordinal) ||
            Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.UnableToVerify,
                ErrorMessage: "Passport verification provider Endpoint must be an absolute-path HTTPS-relative endpoint.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.UnableToVerify,
                ErrorMessage: "Passport verification provider API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, endpoint))
        {
            Content = JsonContent.Create(new PassportVerificationRequest(
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
                return new PassportVerificationResult(
                    PassportVerificationDecision.Error,
                    ErrorMessage: $"Passport verification provider returned HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<PassportVerificationResponse>(cancellationToken);
            if (result is null)
            {
                return new PassportVerificationResult(
                    PassportVerificationDecision.Error,
                    ErrorMessage: "Passport verification provider returned an empty response.");
            }

            if (!Enum.TryParse<PassportVerificationDecision>(result.Decision, ignoreCase: true, out var decision) ||
                decision is PassportVerificationDecision.NotVerified or PassportVerificationDecision.Error)
            {
                return new PassportVerificationResult(
                    PassportVerificationDecision.Error,
                    result.Reference,
                    "Passport verification provider returned an invalid decision.");
            }

            return new PassportVerificationResult(decision, result.Reference, result.ErrorMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.Error,
                ErrorMessage: "Passport verification provider request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.Error,
                ErrorMessage: $"Passport verification provider request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.Error,
                ErrorMessage: $"Passport verification provider returned invalid JSON: {ex.Message}");
        }
    }

    private sealed record PassportVerificationRequest(
        string DocumentNumber,
        string IssuingCountry,
        string Surname,
        string GivenNames,
        DateOnly DateOfBirth,
        string Nationality,
        DateOnly ExpiryDate);

    private sealed record PassportVerificationResponse(
        [property: JsonPropertyName("decision")] string Decision,
        [property: JsonPropertyName("reference")] string? Reference,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
}
