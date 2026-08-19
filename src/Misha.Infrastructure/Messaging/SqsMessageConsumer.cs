using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Misha.Application.Messaging;

namespace Misha.Infrastructure.Messaging;

public sealed class SqsMessageConsumer(
    IAmazonSQS sqs,
    string queueUrl,
    ILogger<SqsMessageConsumer> logger) : ISqsMessageConsumer
{
    public async Task<bool> ConsumeOnceAsync(
        Func<SqsMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = ["All"]
        }, cancellationToken);

        var message = response.Messages?.FirstOrDefault();
        if (message is null)
        {
            return false;
        }

        var applicationMessage = new SqsMessage(
            message.MessageId,
            message.ReceiptHandle,
            message.Body,
            (message.MessageAttributes ?? new Dictionary<string, MessageAttributeValue>())
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.StringValue ?? string.Empty,
                    StringComparer.Ordinal));

        await handler(applicationMessage, cancellationToken);

        await sqs.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        }, cancellationToken);

        logger.LogInformation("Processed and deleted SQS message {MessageId}.", message.MessageId);
        return true;
    }
}
