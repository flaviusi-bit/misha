using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class PostgreSqlApplicationPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("misha_test")
        .WithUsername("misha")
        .WithPassword("misha_test_password")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migrations_create_schema_and_application_lifecycle_survives_new_context()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
            var migrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20260808000000_InitialCreate", migrations);
            Assert.Contains("20260814000000_AddApplicationIdempotency", migrations);
            Assert.Contains("20260814010000_AddApplicationLifecycleAudits", migrations);
            Assert.Contains("20260814020000_AddOutboxMessages", migrations);
            Assert.Contains("20260814030000_AddProcessedEvents", migrations);
            var tables = await db.Database.SqlQueryRaw<string>("select table_name as \"Value\" from information_schema.tables where table_schema = 'public'").ToListAsync();
            Assert.Contains("applications", tables);
            Assert.Contains("application_lifecycle_audits", tables);
            Assert.Contains("outbox_messages", tables);
            Assert.Contains("processed_events", tables);
            Assert.Contains("decision_audits", tables);
            Assert.Contains("watchlist_checks", tables);
        }

        var applicationId = Guid.Empty;
        await using (var db = new MishaDbContext(options))
        {
            var service = CreateService(db);
            applicationId = await service.CreateAsync("integration-app-001", CancellationToken.None);
            await service.SubmitAsync(applicationId, "actor-001", CancellationToken.None);
            await service.StartProcessingAsync(applicationId, "actor-001", CancellationToken.None);
        }
        await using (var db = new MishaDbContext(options))
        {
            await CreateService(db).ApproveAsync(applicationId, "actor-002", CancellationToken.None);
        }
        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            var audits = await db.ApplicationLifecycleAudits.Where(x => x.ApplicationId == applicationId).OrderBy(x => x.OccurredAtUtc).ToListAsync();
            var outboxMessages = await db.OutboxMessages.Where(x => x.AggregateId == applicationId).OrderBy(x => x.OccurredAtUtc).ToListAsync();
            Assert.Equal("integration-app-001", persisted.ApplicantReference);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, persisted.Status);
            Assert.NotNull(persisted.SubmittedAtUtc);
            Assert.NotNull(persisted.ProcessingStartedAtUtc);
            Assert.NotNull(persisted.DecidedAtUtc);
            Assert.Equal(3, audits.Count);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Submitted, audits[0].ToStatus);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Processing, audits[1].ToStatus);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, audits[2].ToStatus);
            Assert.Equal(3, outboxMessages.Count);
            Assert.All(outboxMessages, message => Assert.Equal("application.lifecycle.changed.v1", message.EventType));
            Assert.All(outboxMessages, message => Assert.Null(message.PublishedAtUtc));
            Assert.All(outboxMessages, message => Assert.Equal(0, message.AttemptCount));
        }
    }

    [Fact]
    public async Task Lifecycle_audit_persists_refusal_reason()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid applicationId;
        await using (var db = new MishaDbContext(options))
        {
            var service = CreateService(db);
            applicationId = await service.CreateAsync("integration-app-refusal", CancellationToken.None);
            await service.SubmitAsync(applicationId, "actor-refusal", CancellationToken.None);
            await service.StartProcessingAsync(applicationId, "actor-refusal", CancellationToken.None);
            await service.RefuseAsync(applicationId, "watchlist match", "actor-refusal", CancellationToken.None);
        }
        await using var verificationDb = new MishaDbContext(options);
        var audits = await verificationDb.ApplicationLifecycleAudits.Where(x => x.ApplicationId == applicationId).OrderBy(x => x.OccurredAtUtc).ToListAsync();
        var refusalAudit = audits[^1];
        var refusalEvents = await verificationDb.OutboxMessages
            .Where(x => x.AggregateId == applicationId && x.EventType == "application.lifecycle.changed.v1")
            .ToListAsync();
        var refusalEvent = refusalEvents.Single(x => x.OccurredAtUtc == refusalAudit.OccurredAtUtc);
        Assert.Equal(3, audits.Count);
        Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Refused, refusalAudit.ToStatus);
        Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Processing, refusalAudit.FromStatus);
        Assert.Equal("watchlist match", refusalAudit.Reason);
        Assert.Equal("actor-refusal", refusalAudit.ActorReference);
        Assert.Contains("watchlist match", refusalEvent.Payload);
        using var payload = JsonDocument.Parse(refusalEvent.Payload);
        Assert.Equal("Refused", payload.RootElement.GetProperty("ToStatus").GetString());
    }

    [Fact]
    public async Task Concurrent_lifecycle_update_is_rejected_and_does_not_create_audit()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid applicationId;
        await using (var db = new MishaDbContext(options))
            applicationId = await CreateService(db).CreateAsync("integration-app-concurrency", CancellationToken.None);

        await using var firstDb = new MishaDbContext(options);
        await using var secondDb = new MishaDbContext(options);
        var firstService = CreateService(firstDb);
        var secondService = CreateService(secondDb);
        await secondDb.Applications.SingleAsync(x => x.Id == applicationId);
        await firstService.SubmitAsync(applicationId, "actor-first", CancellationToken.None);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondService.SubmitAsync(applicationId, "actor-second", CancellationToken.None));
        Assert.Single(await firstDb.ApplicationLifecycleAudits.Where(x => x.ApplicationId == applicationId).ToListAsync());
        Assert.Single(await firstDb.OutboxMessages.Where(x => x.AggregateId == applicationId).ToListAsync());
    }

    [Fact]
    public async Task Invalid_transition_is_rejected_without_corrupting_persisted_state()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid applicationId;
        await using (var db = new MishaDbContext(options))
            applicationId = await CreateService(db).CreateAsync("integration-app-002", CancellationToken.None);

        await using (var db = new MishaDbContext(options))
            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(db).ApproveAsync(applicationId, "actor-invalid", CancellationToken.None));

        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Draft, persisted.Status);
            Assert.Null(persisted.DecidedAtUtc);
            Assert.Empty(await db.ApplicationLifecycleAudits.Where(x => x.ApplicationId == applicationId).ToListAsync());
            Assert.Empty(await db.OutboxMessages.Where(x => x.AggregateId == applicationId).ToListAsync());
        }
    }

    [Fact]
    public async Task Reusing_idempotency_key_returns_the_original_application()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid firstId;
        Guid secondId;
        await using (var db = new MishaDbContext(options))
        {
            var service = CreateService(db);
            firstId = await service.CreateAsync("integration-app-idempotent", "request-123", "legacy", CancellationToken.None);
            secondId = await service.CreateAsync("integration-app-idempotent", "request-123", "legacy", CancellationToken.None);
        }
        Assert.Equal(firstId, secondId);
        await using var verificationDb = new MishaDbContext(options);
        Assert.Equal(1, await verificationDb.Applications.CountAsync(x => x.IdempotencyKey == "request-123"));
        Assert.Empty(await verificationDb.OutboxMessages.Where(x => x.AggregateId == firstId).ToListAsync());
    }

    [Fact]
    public async Task Reusing_idempotency_key_for_different_request_is_rejected()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        await using var verificationDb = new MishaDbContext(options);
        var service = CreateService(verificationDb);
        await service.CreateAsync("integration-app-original", "request-456", "legacy", CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync("integration-app-different", "request-456", "legacy", CancellationToken.None));
    }

    private static ApplicationService CreateService(MishaDbContext db) =>
        new(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db), new EfOutboxWriter(db));

    private DbContextOptions<MishaDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MishaDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
}
