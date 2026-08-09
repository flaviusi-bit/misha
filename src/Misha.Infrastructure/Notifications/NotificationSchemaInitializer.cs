using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.Notifications;

public sealed class NotificationSchemaInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationSchemaInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MishaDbContext>();

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS notifications (
                    id uuid NOT NULL,
                    application_id uuid NOT NULL,
                    recipient_reference varchar(200) NOT NULL,
                    channel varchar(32) NOT NULL,
                    template varchar(100) NOT NULL,
                    payload text NOT NULL,
                    status varchar(32) NOT NULL,
                    attempts integer NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    sent_at_utc timestamp with time zone,
                    last_attempt_at_utc timestamp with time zone,
                    last_error varchar(2000),
                    CONSTRAINT pk_notifications PRIMARY KEY (id),
                    CONSTRAINT fk_notifications_applications FOREIGN KEY (application_id)
                        REFERENCES applications (id)
                        ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ix_notifications_status_created
                    ON notifications (status, created_at_utc);
                CREATE INDEX IF NOT EXISTS ix_notifications_application_created
                    ON notifications (application_id, created_at_utc);
                """, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Notification schema initialization failed.");
            throw;
        }
    }
}
