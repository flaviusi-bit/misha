using Microsoft.EntityFrameworkCore;
using Misha.Application.Messaging;
using Misha.Infrastructure.Messaging;
using Misha.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class EventProcessingTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("misha_test")
        .WithUsername("misha")
        .WithPassword("misha_test_password")
        .Build();

    public Task InitializeAsync() => postgres.StartAsync();
    public Task DisposeAsync() => postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task First_processing_executes_handler_and_records_event()
    {
        await MigrateAsync();
        await using var db = CreateDbContext();
        var store = new EfEventIdempotencyStore(db);
        var calls = 0;
        var eventId = Guid.NewGuid();

        var processed = await store.ExecuteOnceAsync(
            eventId,
            "application.lifecycle.changed.v1",
            _ =>
            {
                calls++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(1, calls);
        Assert.Equal(1, await CountProcessedEventsAsync(eventId));
    }

    [Fact]
    public async Task Duplicate_processing_skips_handler()
    {
        await MigrateAsync();
        var eventId = Guid.NewGuid();
        var calls = 0;

        await using (var firstDb = CreateDbContext())
        {
            var store = new EfEventIdempotencyStore(firstDb);
            Assert.True(await store.ExecuteOnceAsync(
                eventId,
                "application.lifecycle.changed.v1",
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                CancellationToken.None));
        }

        await using (var secondDb = CreateDbContext())
        {
            var store = new EfEventIdempotencyStore(secondDb);
            Assert.False(await store.ExecuteOnceAsync(
                eventId,
                "application.lifecycle.changed.v1",
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                CancellationToken.None));
        }

        Assert.Equal(1, calls);
        Assert.Equal(1, await CountProcessedEventsAsync(eventId));
    }

    [Fact]
    public async Task Handler_failure_rolls_back_claim_and_allows_retry()
    {
        await MigrateAsync();
        var eventId = Guid.NewGuid();
        var calls = 0;

        await using (var firstDb = CreateDbContext())
        {
            var store = new EfEventIdempotencyStore(firstDb);
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.ExecuteOnceAsync(
                eventId,
                "application.lifecycle.changed.v1",
                _ =>
                {
                    calls++;
                    throw new InvalidOperationException("transient failure");
                },
                CancellationToken.None));
        }

        Assert.Equal(0, await CountProcessedEventsAsync(eventId));

        await using (var retryDb = CreateDbContext())
        {
            var store = new EfEventIdempotencyStore(retryDb);
            Assert.True(await store.ExecuteOnceAsync(
                eventId,
                "application.lifecycle.changed.v1",
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                CancellationToken.None));
        }

        Assert.Equal(2, calls);
        Assert.Equal(1, await CountProcessedEventsAsync(eventId));
    }

    [Fact]
    public async Task Concurrent_duplicate_processing_executes_handler_once()
    {
        await MigrateAsync();
        var eventId = Guid.NewGuid();
        var calls = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var firstDb = CreateDbContext();
        await using var secondDb = CreateDbContext();
        var firstStore = new EfEventIdempotencyStore(firstDb);
        var secondStore = new EfEventIdempotencyStore(secondDb);

        async Task Handler(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            await gate.Task.WaitAsync(cancellationToken);
        }

        var first = firstStore.ExecuteOnceAsync(
            eventId,
            "application.lifecycle.changed.v1",
            Handler,
            CancellationToken.None);
        var second = secondStore.ExecuteOnceAsync(
            eventId,
            "application.lifecycle.changed.v1",
            Handler,
            CancellationToken.None);

        await Task.Delay(100);
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(x => x));
        Assert.Equal(1, results.Count(x => !x));
        Assert.Equal(1, calls);
        Assert.Equal(1, await CountProcessedEventsAsync(eventId));
    }

    [Fact]
    public async Task Reusing_event_id_for_a_different_type_is_rejected()
    {
        await MigrateAsync();
        var eventId = Guid.NewGuid();

        await using (var db = CreateDbContext())
        {
            var store = new EfEventIdempotencyStore(db);
            Assert.True(await store.ExecuteOnceAsync(
                eventId,
                "application.lifecycle.changed.v1",
                _ => Task.CompletedTask,
                CancellationToken.None));
        }

        await using (var db = CreateDbContext())
        {
            var store = new EfEventIdempotencyStore(db);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.ExecuteOnceAsync(
                eventId,
                "different.event.v1",
                _ => Task.CompletedTask,
                CancellationToken.None));

            Assert.Contains("application.lifecycle.changed.v1", exception.Message);
            Assert.Contains("different.event.v1", exception.Message);
        }
    }

    private async Task MigrateAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    private async Task<int> CountProcessedEventsAsync(Guid eventId)
    {
        await using var db = CreateDbContext();
        return await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::integer AS "Value"
            FROM processed_events
            WHERE event_id = {eventId};
            """).SingleAsync();
    }

    private MishaDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<MishaDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options);
}
