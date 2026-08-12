using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Misha.Integration.Tests;

public sealed class PostgreSqlApplicationPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
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

            var tables = await db.Database.SqlQueryRaw<string>(
                "select table_name as \"Value\" from information_schema.tables where table_schema = 'public'").ToListAsync();

            Assert.Contains("applications", tables);
            Assert.Contains("decision_audits", tables);
            Assert.Contains("watchlist_checks", tables);
        }

        var applicationId = Guid.Empty;

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var service = new ApplicationService(repository);

            applicationId = await service.CreateAsync("integration-app-001", CancellationToken.None);
            await service.SubmitAsync(applicationId, CancellationToken.None);
            await service.StartProcessingAsync(applicationId, CancellationToken.None);
        }

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var service = new ApplicationService(repository);

            await service.ApproveAsync(applicationId, CancellationToken.None);
        }

        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);

            Assert.Equal("integration-app-001", persisted.ApplicantReference);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Approved, persisted.Status);
            Assert.NotNull(persisted.SubmittedAtUtc);
            Assert.NotNull(persisted.ProcessingStartedAtUtc);
            Assert.NotNull(persisted.DecidedAtUtc);
        }
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
            var service = new ApplicationService(repository);
            applicationId = await service.CreateAsync("integration-app-002", CancellationToken.None);
        }

        await using (var db = new MishaDbContext(options))
        {
            var repository = new EfApplicationRepository(db);
            var service = new ApplicationService(repository);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ApproveAsync(applicationId, CancellationToken.None));
        }

        await using (var db = new MishaDbContext(options))
        {
            var persisted = await db.Applications.SingleAsync(x => x.Id == applicationId);
            Assert.Equal(Misha.Domain.Applications.ApplicationStatus.Draft, persisted.Status);
            Assert.Null(persisted.DecidedAtUtc);
        }
    }

    private DbContextOptions<MishaDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MishaDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
}
