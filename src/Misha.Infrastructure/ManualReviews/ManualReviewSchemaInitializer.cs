using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.ManualReviews;

public sealed class ManualReviewSchemaInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<ManualReviewSchemaInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MishaDbContext>();

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS manual_review_cases (
                    id uuid NOT NULL,
                    application_id uuid NOT NULL,
                    status varchar(32) NOT NULL,
                    trigger varchar(100) NOT NULL,
                    reason varchar(2000) NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    assigned_to_actor_reference varchar(200),
                    assigned_at_utc timestamp with time zone,
                    resolution varchar(32),
                    resolution_reason varchar(2000),
                    resolved_by_actor_reference varchar(200),
                    resolved_at_utc timestamp with time zone,
                    CONSTRAINT pk_manual_review_cases PRIMARY KEY (id),
                    CONSTRAINT fk_manual_review_cases_applications FOREIGN KEY (application_id)
                        REFERENCES applications (id)
                        ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ix_manual_review_cases_status_created
                    ON manual_review_cases (status, created_at_utc);

                CREATE INDEX IF NOT EXISTS ix_manual_review_cases_application
                    ON manual_review_cases (application_id, created_at_utc);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_manual_review_cases_open_application
                    ON manual_review_cases (application_id)
                    WHERE status IN ('Pending', 'InProgress');
                """, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Manual review schema initialization failed.");
            throw;
        }
    }
}
