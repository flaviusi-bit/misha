# Notifications

## Delivery model

MISHA persists notifications before delivery. Application lifecycle events create durable notification records, and the worker delivers pending or previously failed records through a configurable HTTP adapter.

```text
Application lifecycle event
        |
        v
Notification queue (PostgreSQL)
        |
        v
NotificationDeliveryWorker
        |
        v
Configurable HTTP provider
```

## Provider boundary

The application does not depend on a specific email/SMS vendor. `INotificationDelivery` is the application boundary and `HttpNotificationDelivery` is the infrastructure adapter.

Configuration is supplied through `Notifications:Delivery`:

- `Enabled` — enables delivery processing
- `Endpoint` — provider endpoint
- `ApiKey` — optional provider credential; do not commit secrets
- `BatchSize` — maximum notifications processed per cycle

The delivery request includes an `Idempotency-Key` derived from the notification ID so a provider can safely deduplicate retries.

## Reliability

- Pending and failed notifications are retried by the worker.
- Successful delivery is persisted as `Sent`.
- Failed delivery records the attempt count, timestamp and error.
- The worker does not log notification payloads or recipient references.

Provider selection, credential provisioning, template/content policy, bounce handling and channel-specific contracts remain deployment-specific production gates.
