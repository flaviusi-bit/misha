using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Misha.Application.Notifications;
using Misha.Domain.Notifications;

namespace Misha.Infrastructure.Notifications;

public sealed class HttpNotificationDelivery(
    HttpClient httpClient,
    IOptions<NotificationDeliveryOptions> options) : INotificationDelivery
{
    public async Task DeliverAsync(Notification notification, CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        if (!configuration.Enabled)
            throw new InvalidOperationException("Notification delivery is disabled.");
        if (string.IsNullOrWhiteSpace(configuration.Endpoint))
            throw new InvalidOperationException("Notification delivery endpoint is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.Endpoint)
        {
            Content = JsonContent.Create(new
            {
                notificationId = notification.Id,
                applicationId = notification.ApplicationId,
                recipientReference = notification.RecipientReference,
                channel = notification.Channel,
                template = notification.Template,
                payload = notification.Payload
            })
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", notification.Id.ToString("N"));

        if (!string.IsNullOrWhiteSpace(configuration.ApiKey))
            request.Headers.TryAddWithoutValidation("X-API-Key", configuration.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Notification delivery endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}.");
    }
}
