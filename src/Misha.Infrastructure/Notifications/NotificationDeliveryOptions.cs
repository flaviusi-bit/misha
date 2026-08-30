namespace Misha.Infrastructure.Notifications;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 20;
}
