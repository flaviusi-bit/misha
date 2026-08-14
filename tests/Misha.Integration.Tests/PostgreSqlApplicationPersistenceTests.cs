using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class PostgreSqlApplicationPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").WithDatabase("misha_test").WithUsername("misha").WithPassword("misha_test_password").Build();
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
            var tables = await db.Database.SqlQueryRaw<string>("select table_name as \"Value\" from information_schema.tables where table_schema = 'public'").ToListAsync();
            Assert.Contains("applications", tables); Assert.Contains("application_lifecycle_audits", tables); Assert.Contains("decision_audits", tables); Assert.Contains("watchlist_checks", tables);
        }

        var applicationId = Guid.Empty;
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            applicationId = await service.CreateAsync("integration-app-001", "request-001", "integration-test", CancellationToken.None);
            await service.SubmitAsync(applicationId, "integration-test", CancellationToken.None);
            await service.StartProcessingAsync(applicationId, "integration-test", CancellationToken.None);
        }
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            await service.ApproveAsync(applicationId, "integration-test", CancellationToken.None);
        }
        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, persisted.Status);
            var audits = await new EfApplicationLifecycleAuditRepository(db).GetByApplicationAsync(applicationId, CancellationToken.None);
            Assert.Equal(3, audits.Count);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Draft, audits[0].FromStatus); Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Submitted, audits[0].ToStatus);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Submitted, audits[1].FromStatus); Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Processing, audits[1].ToStatus);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Processing, audits[2].FromStatus); Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, audits[2].ToStatus);
            Assert.All(audits, audit => Assert.Equal("integration-test", audit.ActorReference));
        }
    }

    [Fact]
    public async Task Invalid_transition_is_rejected_without_corrupting_persisted_state_or_audit_history()
    {
        var options = CreateOptions(); await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid applicationId;
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            applicationId = await service.CreateAsync("integration-app-002", null, "integration-test", CancellationToken.None);
        }
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(applicationId, "integration-test", CancellationToken.None));
        }
        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Draft, persisted.Status);
            Assert.Empty(await new EfApplicationLifecycleAuditRepository(db).GetByApplicationAsync(applicationId, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Optimistic_concurrency_rejects_stale_application_update()
    {
        var options = CreateOptions(); await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid applicationId;
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            applicationId = await service.CreateAsync("integration-app-concurrency", null, "integration-test", CancellationToken.None);
        }
        await using var firstContext = new MishaDbContext(options); await using var secondContext = new MishaDbContext(options);
        var first = await firstContext.Applications.SingleAsync(x => x.Id == applicationId); var second = await secondContext.Applications.SingleAsync(x => x.Id == applicationId);
        first.Submit(); await firstContext.SaveChangesAsync(); second.Submit(); await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Reusing_idempotency_key_returns_the_original_application()
    {
        var options = CreateOptions(); await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        Guid firstId, secondId;
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            firstId = await service.CreateAsync("integration-app-idempotent", "request-123", "integration-test", CancellationToken.None);
            secondId = await service.CreateAsync("integration-app-idempotent", "request-123", "integration-test", CancellationToken.None);
        }
        Assert.Equal(firstId, secondId); await using var verificationDb = new MishaDbContext(options); Assert.Equal(1, await verificationDb.Applications.CountAsync(x => x.IdempotencyKey == "request-123")); Assert.Empty(await verificationDb.ApplicationLifecycleAudits.Where(x => x.ApplicationId == firstId).ToListAsync());
    }

    [Fact]
    public async Task Reusing_idempotency_key_for_different_request_is_rejected()
    {
        var options = CreateOptions(); await using (var db = new MishaDbContext(options)) await db.Database.MigrateAsync();
        await using (var db = new MishaDbContext(options))
        {
            var service = new ApplicationService(new EfApplicationRepository(db), new EfApplicationLifecycleAuditRepository(db));
            await service.CreateAsync("integration-app-original", "request-456", "integration-test", CancellationToken.None);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync("integration-app-different", "request-456", "integration-test", CancellationToken.None));
        }
    }

    private DbContextOptions<MishaDbContext> CreateOptions() => new DbContextOptionsBuilder<MishaDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
}
