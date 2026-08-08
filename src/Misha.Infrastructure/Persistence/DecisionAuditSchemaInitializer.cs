using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Misha.Infrastructure.Persistence;

public sealed class DecisionAuditSchemaInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MishaDbContext>();

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS decision_audits (
                "Id" uuid NOT NULL,
                "ApplicationId" uuid NOT NULL,
                "PolicyVersion" character varying(50) NOT NULL,
                "PolicyDecision" character varying(32) NOT NULL,
                "Decision" character varying(32) NOT NULL,
                "ReasonsJson" jsonb NOT NULL,
                "ActorReference" character varying(200) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_decision_audits" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_decision_audits_ApplicationId_CreatedAtUtc"
            ON decision_audits ("ApplicationId", "CreatedAtUtc");
            """, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
