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

            var tables = await db.Database.SqlQueryRaw<string>(
                "select table_name as \"Value\" from information_schema.tables where table_schema = 'public'").ToListAsync();

            Assert.Contains("applications", tables);
            Assert.Contains("application_lifecycle_audits", tables);
            Assert.Contains("decision_audits", tables);
            Assert.Contains("watchlist_checks", tables);
        }

        var applicationId = Guid.Empty;

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);

            applicationId = await service.CreateAsync("integration-app-001", CancellationToken.None);
            await service.SubmitAsync(applicationId, "actor-001", CancellationToken.None);
            await service.StartProcessingAsync(applicationId, "actor-001", CancellationToken.None);
        }

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);

            await service.ApproveAsync(applicationId, "actor-002", CancellationToken.None);
        }

        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            var audits = await db.ApplicationLifecycleAudits
                .Where(x => x.ApplicationId == applicationId)
                .OrderBy(x => x.OccurredAtUtc)
                .ToListAsync();

            Assert.Equal("integration-app-001", persisted.ApplicantReference);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, persisted.Status);
            Assert.NotNull(persisted.SubmittedAtUtc);
            Assert.NotNull(persisted.ProcessingStartedAtUtc);
            Assert.NotNull(persisted.DecidedAtUtc);

            Assert.Equal(3, audits.Count);
            Assert.Equal("actor-001", audits[0].ActorReference);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Submitted, audits[0].ToStatus);
            Assert.Equal("actor-001", audits[1].ActorReference);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Processing, audits[1].ToStatus);
            Assert.Equal("actor-002", audits[2].ActorReference);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, audits[2].ToStatus);
        }
    }

    [Fact]
    public async Task Lifecycle_audit_persists_refusal_reason()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Guid applicationId;
        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);

            applicationId = await service.CreateAsync("integration-app-refusal", CancellationToken.None);
            await service.SubmitAsync(applicationId, "actor-refusal", CancellationToken.None);
            await service.StartProcessingAsync(applicationId, "actor-refusal", CancellationToken.None);
            await service.RefuseAsync(applicationId, "watchlist match", "actor-refusal", CancellationToken.None);
        }

        await using var verificationDb = new MishaDbContext(options);
        var audit = await verificationDb.ApplicationLifecycleAudits
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .SingleAsync();

        Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Refused, audit.ToStatus);
        Assert.Equal("watchlist match", audit.Reason);
        Assert.Equal("actor-refusal", audit.ActorReference);
    }

    [Fact]
    public async Task Concurrent_lifecycle_update_is_rejected_and_does_not_create_audit()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Guid applicationId;
        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);
            applicationId = await service.CreateAsync("integration-app-concurrency", CancellationToken.None);
        }

        await using var firstDb = new MishaDbContext(options);
        await using var secondDb = new MishaDbContext(options);

        var firstService = new ApplicationService(
            new EfApplicationRepository(firstDb),
            new EfApplicationLifecycleAuditRepository(firstDb));
        var secondService = new ApplicationService(
            new EfApplicationRepository(secondDb),
            new EfApplicationLifecycleAuditRepository(secondDb));

        // Load the second copy before the first transaction changes the row. This creates
        // the stale xmin concurrency token that the test is intended to exercise.
        await secondDb.Applications.SingleAsync(x => x.Id == applicationId);

        await firstService.SubmitAsync(applicationId, "actor-first", CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            secondService.SubmitAsync(applicationId, "actor-second", CancellationToken.None));

        var audits = await firstDb.ApplicationLifecycleAudits
            .Where(x => x.ApplicationId == applicationId)
            .ToListAsync();
        Assert.Single(audits);
        Assert.Equal("actor-first", audits[0].ActorReference);
    }

    [Fact]
    public async Task Invalid_transition_is_rejected_without_corrupting_persisted_state()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Guid applicationId;
        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);
            applicationId = await service.CreateAsync("integration-app-002", CancellationToken.None);
        }

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ApproveAsync(applicationId, "actor-invalid", CancellationToken.None));
        }

        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            var audits = await db.ApplicationLifecycleAudits
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();

            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Draft, persisted.Status);
            Assert.Null(persisted.DecidedAtUtc);
            Assert.Empty(audits);
        }
    }

    [Fact]
    public async Task Reusing_idempotency_key_returns_the_original_application()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Guid firstId;
        Guid secondId;
        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);

            firstId = await service.CreateAsync("integration-app-idempotent", "request-123", CancellationToken.None);
            secondId = await service.CreateAsync("integration-app-idempotent", "request-123", CancellationToken.None);
        }

        Assert.Equal(firstId, secondId);

        await using var verificationDb = new MishaDbContext(options);
        Assert.Equal(1, await verificationDb.Applications.CountAsync(x => x.IdempotencyKey == "request-123"));
    }

    [Fact]
    public async Task Reusing_idempotency_key_for_different_request_is_rejected()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var lifecycleAudits = new EfApplicationLifecycleAuditRepository(db);
            var service = new ApplicationService(repository, lifecycleAudits);

            await service.CreateAsync("integration-app-original", "request-456", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync("integration-app-different", "request-456", CancellationToken.None));
        }
    }

    private DbContextOptions<MishaDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MishaDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
}
