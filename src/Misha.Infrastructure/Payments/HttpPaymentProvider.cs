using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Misha.Application.Payments;
using Misha.Domain.Payments;

namespace Misha.Infrastructure.Payments;

public sealed class HttpPaymentProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IPaymentProvider
{
    private const string ClientName = "payment-provider";

    public string Name => configuration["Payment:ProviderName"]?.Trim() is { Length: > 0 } name
        ? name
        : "configured-http";

    public async Task<PaymentProviderResult> CreateAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Payment:BaseUrl"]?.Trim();
        var endpoint = configuration["Payment:Endpoint"]?.Trim() ?? "/payments";
        var apiKey = configuration["Payment:ApiKey"]?.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: "Payment provider is not configured with an HTTPS BaseUrl.");
        }

        if (string.IsNullOrWhiteSpace(endpoint) || !endpoint.StartsWith("/", StringComparison.Ordinal) ||
            Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: "Payment provider Endpoint must be an absolute-path HTTPS-relative endpoint.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: "Payment provider API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, endpoint))
        {
            Content = JsonContent.Create(new PaymentCreateRequest(
                payment.Id,
                payment.ApplicationId,
                payment.AmountMinor,
                payment.Currency))
        };
        request.Headers.Add("X-API-Key", apiKey);

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentProviderResult(
                    PaymentStatus.Failed,
                    ErrorMessage: $"Payment provider returned HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<PaymentCreateResponse>(cancellationToken);
            if (result is null)
            {
                return new PaymentProviderResult(
                    PaymentStatus.Failed,
                    ErrorMessage: "Payment provider returned an empty response.");
            }

            if (!Enum.TryParse<PaymentStatus>(result.Status, ignoreCase: true, out var status) ||
                status is PaymentStatus.Cancelled)
            {
                return new PaymentProviderResult(
                    PaymentStatus.Failed,
                    result.Reference,
                    ErrorMessage: "Payment provider returned an invalid status.");
            }

            if (status == PaymentStatus.RequiresAction)
            {
                if (!Uri.TryCreate(result.ActionUrl, UriKind.Absolute, out var actionUri) ||
                    actionUri.Scheme != Uri.UriSchemeHttps)
                {
                    return new PaymentProviderResult(
                        PaymentStatus.Failed,
                        result.Reference,
                        ErrorMessage: "Payment provider returned an invalid HTTPS action URL.");
                }
            }

            return new PaymentProviderResult(
                status,
                result.Reference,
                result.ActionUrl,
                result.ErrorMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: "Payment provider request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: $"Payment provider request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: $"Payment provider returned invalid JSON: {ex.Message}");
        }
    }

    private sealed record PaymentCreateRequest(
        Guid PaymentId,
        Guid ApplicationId,
        long AmountMinor,
        string Currency);

    private sealed record PaymentCreateResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("reference")] string? Reference,
        [property: JsonPropertyName("actionUrl")] string? ActionUrl,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
}
