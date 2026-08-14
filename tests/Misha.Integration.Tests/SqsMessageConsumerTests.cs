using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Misha.Infrastructure.Messaging;
using Moq;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class SqsMessageConsumerTests
{
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/application-events";

    [Fact]
    public async Task ConsumeOnceAsync_WithMessage_InvokesHandlerAndDeletesMessage()
    {
        var sqs = new Mock<IAmazonSQS>();
        var message = CreateMessage();
        sqs.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = [message] });
        sqs.Setup(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var consumer = CreateConsumer(sqs);
        var received = new List<Misha.Application.Messaging.SqsMessage>();

        var consumed = await consumer.ConsumeOnceAsync((item, _) =>
        {
            received.Add(item);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.True(consumed);
        var item = Assert.Single(received);
        Assert.Equal(message.MessageId, item.MessageId);
        Assert.Equal(message.ReceiptHandle, item.ReceiptHandle);
        Assert.Equal(message.Body, item.Body);
        Assert.Equal("LifecycleChanged", item.Attributes["eventType"]);
        Assert.Equal("42", item.Attributes["eventId"]);

        sqs.Verify(x => x.DeleteMessageAsync(
            It.Is<DeleteMessageRequest>(request =>
                request.QueueUrl == QueueUrl && request.ReceiptHandle == message.ReceiptHandle),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConsumeOnceAsync_WhenHandlerFails_DoesNotDeleteMessage()
    {
        var sqs = new Mock<IAmazonSQS>();
        var message = CreateMessage();
        sqs.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = [message] });

        var consumer = CreateConsumer(sqs);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            consumer.ConsumeOnceAsync((_, _) =>
                Task.FromException(new InvalidOperationException("processing failed")), CancellationToken.None));

        sqs.Verify(x => x.DeleteMessageAsync(
            It.IsAny<DeleteMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConsumeOnceAsync_WhenQueueIsEmpty_ReturnsFalseWithoutDelete()
    {
        var sqs = new Mock<IAmazonSQS>();
        sqs.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = [] });

        var consumer = CreateConsumer(sqs);
        var handled = false;

        var consumed = await consumer.ConsumeOnceAsync((_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.False(consumed);
        Assert.False(handled);
        sqs.Verify(x => x.DeleteMessageAsync(
            It.IsAny<DeleteMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SqsMessageConsumer CreateConsumer(Mock<IAmazonSQS> sqs) =>
        new(sqs.Object, QueueUrl, NullLogger<SqsMessageConsumer>.Instance);

    private static Message CreateMessage() => new()
    {
        MessageId = "message-1",
        ReceiptHandle = "receipt-1",
        Body = "{\"event\":\"lifecycle\"}",
        MessageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["eventType"] = new() { DataType = "String", StringValue = "LifecycleChanged" },
            ["eventId"] = new() { DataType = "String", StringValue = "42" }
        }
    };
}
