using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Misha.Infrastructure.Persistence;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class SqsOutboxDispatcherTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("misha_test")
        .WithUsername("misha")
        .WithPassword("misha_test_password")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Dispatch_pending_publishes_oldest_messages_with_metadata_and_marks_them_published()
    {
        var options = CreateOptions();
        Guid olderId;
        Guid newerId;
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
            olderId = Guid.NewGuid();
            newerId = Guid.NewGuid();
            db.OutboxMessages.AddRange(
                NewMessage(olderId, DateTimeOffset.UtcNow.AddMinutes(-2), "older-payload"),
                NewMessage(newerId, DateTimeOffset.UtcNow.AddMinutes(-1), "newer-payload"));
            await db.SaveChangesAsync();
        }

        var aggregateId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var requests = new List<SendMessageRequest>();
        var sqs = new Mock<IAmazonSQS>();
        sqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((request, _) => requests.Add(request))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        await using (var db = new MishaDbContext(options))
        {
            var dispatcher = new SqsOutboxDispatcher(db, sqs.Object, "https://sqs.example.test/application-events", NullLogger<SqsOutboxDispatcher>.Instance);
            var published = await dispatcher.DispatchPendingAsync(CancellationToken.None);

            Assert.Equal(2, published);
            Assert.Equal(2, requests.Count);
            Assert.Equal("older-payload", JsonDocument.Parse(requests[0].MessageBody).RootElement.GetProperty("value").GetString());
            Assert.Equal("newer-payload", JsonDocument.Parse(requests[1].MessageBody).RootElement.GetProperty("value").GetString());
            Assert.Equal("https://sqs.example.test/application-events", requests[0].QueueUrl);
            Assert.Equal("application.lifecycle.changed.v1", requests[0].MessageAttributes["eventType"].StringValue);
            Assert.Equal(aggregateId.ToString(), requests[0].MessageAttributes["aggregateId"].StringValue);
            Assert.Equal(olderId.ToString(), requests[0].MessageAttributes["eventId"].StringValue);

            var persisted = await db.OutboxMessages.OrderBy(x => x.OccurredAtUtc).ToListAsync();
            Assert.All(persisted, message => Assert.NotNull(message.PublishedAtUtc));
        }
    }

    [Fact]
    public async Task Dispatch_pending_limits_batch_to_ten_and_leaves_remaining_messages_pending()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
            for (var i = 0; i < 11; i++)
            {
                db.OutboxMessages.Add(NewMessage(Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(i), $"payload-{i}"));
            }
            await db.SaveChangesAsync();
        }

        var sqs = new Mock<IAmazonSQS>();
        sqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        await using (var db = new MishaDbContext(options))
        {
            var dispatcher = new SqsOutboxDispatcher(db, sqs.Object, "queue-url", NullLogger<SqsOutboxDispatcher>.Instance);
            Assert.Equal(10, await dispatcher.DispatchPendingAsync(CancellationToken.None));
        }

        await using var verificationDb = new MishaDbContext(options);
        Assert.Equal(10, await verificationDb.OutboxMessages.CountAsync(x => x.PublishedAtUtc != null));
        Assert.Single(await verificationDb.OutboxMessages.Where(x => x.PublishedAtUtc == null).ToListAsync());
    }

    [Fact]
    public async Task Sqs_failure_leaves_message_pending()
    {
        var options = CreateOptions();
        Guid messageId;
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
            messageId = Guid.NewGuid();
            db.OutboxMessages.Add(NewMessage(messageId, DateTimeOffset.UtcNow, "payload"));
            await db.SaveChangesAsync();
        }

        var sqs = new Mock<IAmazonSQS>();
        sqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("SQS unavailable"));

        await using (var db = new MishaDbContext(options))
        {
            var dispatcher = new SqsOutboxDispatcher(db, sqs.Object, "queue-url", NullLogger<SqsOutboxDispatcher>.Instance);
            await Assert.ThrowsAsync<AmazonSQSException>(() => dispatcher.DispatchPendingAsync(CancellationToken.None));
        }

        await using var verificationDb = new MishaDbContext(options);
        var persisted = await verificationDb.OutboxMessages.SingleAsync(x => x.Id == messageId);
        Assert.Null(persisted.PublishedAtUtc);
    }

    [Fact]
    public async Task Already_published_messages_are_not_sent_again()
    {
        var options = CreateOptions();
        await using (var db = new MishaDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.OutboxMessages.Add(NewMessage(Guid.NewGuid(), DateTimeOffset.UtcNow, "published-payload", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var sqs = new Mock<IAmazonSQS>();
        await using (var db = new MishaDbContext(options))
        {
            var dispatcher = new SqsOutboxDispatcher(db, sqs.Object, "queue-url", NullLogger<SqsOutboxDispatcher>.Instance);
            Assert.Equal(0, await dispatcher.DispatchPendingAsync(CancellationToken.None));
        }

        sqs.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static OutboxMessage NewMessage(Guid id, DateTimeOffset occurredAtUtc, string payload, DateTimeOffset? publishedAtUtc = null) => new()
    {
        Id = id,
        EventType = "application.lifecycle.changed.v1",
        AggregateId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Payload = JsonSerializer.Serialize(new { value = payload }),
        OccurredAtUtc = occurredAtUtc,
        PublishedAtUtc = publishedAtUtc,
        AttemptCount = 0
    };

    private DbContextOptions<MishaDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MishaDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
}
