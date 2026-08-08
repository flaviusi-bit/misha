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
        var endpoint = configuration["Payment:Endpoint"]?.Trim() ?? "/payments";
        if (!TryCreateRequestUri(endpoint, out var uri, out var error))
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: error);

        var apiKey = configuration["Payment:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: "Payment provider API key is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
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
            return await ReadProviderResultAsync(response, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: "Payment provider request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: $"Payment provider request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: $"Payment provider returned invalid JSON: {ex.Message}");
        }
    }

    public async Task<PaymentProviderResult> GetStatusAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payment.ProviderReference))
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: "Payment provider reference is required for reconciliation.");

        var template = configuration["Payment:StatusEndpoint"]?.Trim() ?? "/payments/{reference}";
        var endpoint = template.Replace(
            "{reference}",
            Uri.EscapeDataString(payment.ProviderReference),
            StringComparison.Ordinal);

        if (!TryCreateRequestUri(endpoint, out var uri, out var error))
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: error);

        var apiKey = configuration["Payment:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: "Payment provider API key is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-API-Key", apiKey);

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            using var response = await client.SendAsync(request, cancellationToken);
            return await ReadProviderResultAsync(response, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PaymentProviderResult(PaymentStatus.Pending, ErrorMessage: "Payment provider reconciliation request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new PaymentProviderResult(PaymentStatus.Pending, ErrorMessage: $"Payment provider reconciliation failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new PaymentProviderResult(PaymentStatus.Pending, ErrorMessage: $"Payment provider returned invalid JSON: {ex.Message}");
        }
    }

    private async Task<PaymentProviderResult> ReadProviderResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                ErrorMessage: $"Payment provider returned HTTP {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<PaymentProviderResponse>(cancellationToken);
        if (result is null)
            return new PaymentProviderResult(PaymentStatus.Failed, ErrorMessage: "Payment provider returned an empty response.");

        if (!Enum.TryParse<PaymentStatus>(result.Status, ignoreCase: true, out var status) ||
            status is PaymentStatus.Cancelled)
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                result.Reference,
                ErrorMessage: "Payment provider returned an invalid status.");
        }

        if (status == PaymentStatus.RequiresAction &&
            (!Uri.TryCreate(result.ActionUrl, UriKind.Absolute, out var actionUri) ||
             actionUri.Scheme != Uri.UriSchemeHttps))
        {
            return new PaymentProviderResult(
                PaymentStatus.Failed,
                result.Reference,
                ErrorMessage: "Payment provider returned an invalid HTTPS action URL.");
        }

        return new PaymentProviderResult(
            status,
            result.Reference,
            result.ActionUrl,
            result.ErrorMessage);
    }

    private bool TryCreateRequestUri(string endpoint, out Uri uri, out string error)
    {
        uri = null!;
        error = string.Empty;

        var baseUrl = configuration["Payment:BaseUrl"]?.Trim();
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            error = "Payment provider is not configured with an HTTPS BaseUrl.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(endpoint) || !endpoint.StartsWith("/", StringComparison.Ordinal) ||
            Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            error = "Payment provider endpoint must be an absolute-path HTTPS-relative endpoint.";
            return false;
        }

        uri = new Uri(baseUri, endpoint);
        return true;
    }

    private sealed record PaymentCreateRequest(
        Guid PaymentId,
        Guid ApplicationId,
        long AmountMinor,
        string Currency);

    private sealed record PaymentProviderResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("reference")] string? Reference,
        [property: JsonPropertyName("actionUrl")] string? ActionUrl,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
}
