using Misha.Application.Notifications;

namespace Misha.Api;

public static class NotificationEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/applications/{id:guid}/notifications", async (
            Guid id,
            QueueNotificationRequest request,
            NotificationService service,
            CancellationToken ct) =>
        {
            var validation = ApiRequestValidation.ValidateNotification(request);
            if (validation is not null)
                return Results.ValidationProblem(validation);

            try
            {
                var notificationId = await service.QueueAsync(
                    id,
                    request.RecipientReference,
                    request.Channel,
                    request.Template,
                    request.Payload,
                    ct);
                return Results.Accepted($"/notifications/{notificationId}", new { id = notificationId });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiWrite);

        app.MapGet("/admin/notifications/pending", async (
            int? limit,
            NotificationService service,
            CancellationToken ct) =>
        {
            var boundedLimit = ApiRequestValidation.NormalizePageSize(
                limit,
                ApiRequestValidation.DefaultNotificationPageSize,
                ApiRequestValidation.MaxNotificationPageSize);
            var notifications = await service.GetPendingAsync(boundedLimit, ct);
            return Results.Ok(notifications.Select(x => new
            {
                x.Id,
                x.ApplicationId,
                x.RecipientReference,
                x.Channel,
                x.Template,
                x.Payload,
                x.Status,
                x.Attempts,
                x.CreatedAtUtc,
                x.LastAttemptAtUtc,
                x.LastError
            }));
        }).RequireAuthorization(AuthorizationPolicies.AdminRead);
    }
}

public sealed record QueueNotificationRequest(
    string RecipientReference,
    string Channel,
    string Template,
    string Payload);
