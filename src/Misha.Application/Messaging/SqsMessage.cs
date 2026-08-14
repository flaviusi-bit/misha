namespace Misha.Application.Messaging;

public sealed record SqsMessage(
    string MessageId,
    string ReceiptHandle,
    string Body,
    IReadOnlyDictionary<string, string> Attributes);
