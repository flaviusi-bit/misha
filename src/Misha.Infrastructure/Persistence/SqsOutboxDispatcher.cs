using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Misha.Application.Messaging;

namespace Misha.Infrastructure.Persistence;

public sealed class SqsOutboxDispatcher(
    MishaDbContext db,
    IAmazonSQS sqs,
    string queueUrl,
    ILogger<SqsOutboxDispatcher> logger) : IOutboxDispatcher
{
    private const int BatchSize = 10;

    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        var messages = await db.OutboxMessages
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = message.Payload,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["eventType"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = message.EventType
                    },
                    ["eventId"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = message.Id.ToString()
                    },
                    ["aggregateId"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = message.AggregateId.ToString()
                    }
                }
            }, cancellationToken);

            message.PublishedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            published++;

            logger.LogInformation(
                "Published outbox event {EventId} of type {EventType} for aggregate {AggregateId}.",
                message.Id,
                message.EventType,
                message.AggregateId);
        }

        return published;
    }
}
