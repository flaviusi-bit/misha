# Event processing and idempotency

## Contract

SQS delivery is treated as **at-least-once**. A message may be delivered more than once, so business processing must be idempotent by the published `eventId`.

The processing boundary is:

```text
SQS message
  -> validate eventId + eventType
  -> select event handler
  -> atomically claim eventId
  -> execute handler
  -> commit processed-event record
  -> SQS consumer deletes message
```

A duplicate `eventId` is acknowledged as already processed and must not execute the handler again.

## Failure semantics

The idempotency claim and handler execution share one PostgreSQL transaction. If the handler fails, the transaction rolls back and the event remains eligible for SQS retry. A later delivery can claim the event and execute it again.

The unique primary key on `processed_events.event_id` also serializes concurrent duplicate deliveries: exactly one concurrent transaction can claim a new event id.

An existing `eventId` associated with a different event type is treated as an integrity error and is not acknowledged.

## Scope boundary

This slice establishes the processing contract and persistence primitive. It does not yet attach domain-specific lifecycle handlers to the worker loop or claim that external side effects are transactionally atomic. External integrations require their own idempotency contracts.
