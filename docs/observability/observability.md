# Observability Architecture

## Three pillars

- Logs
- Metrics
- Traces

## Correlation

Every request and asynchronous operation carries traceId, correlationId and causationId where applicable.

## Structured logs

Logs use JSON and include timestamp, level, service, environment, traceId, correlationId, operation, applicationId, duration and result as appropriate.

Sensitive passport, biometric, watchlist and payment data is excluded or redacted.

## Application metrics

Initial business metrics:

- applications.created
- applications.submitted
- applications.processing
- decisions.completed
- etas.issued
- payments.failed

## Integration metrics

- watchlist.requests
- watchlist.failures
- passport.requests
- passport.failures
- biometric.requests
- biometric.failures
- payment.provider.failures

## Queue metrics

Monitor queue depth, message age, processing rate, retry count and dead-letter queue depth.

## API metrics

Monitor request count, latency, 4xx, 5xx and rate limiting.

## Database metrics

Monitor connections, CPU, storage, latency, locks, slow queries and failover health.

## Business SLIs

Initial candidates:

- Application processing completion rate
- Processing latency p50/p95
- Payment success rate
- eTA issuance success rate
- Watchlist availability

## Critical alerts

- API 5xx spike
- Processing backlog
- DLQ growth
- Database unavailable
- Watchlist unavailable
- Payment failure spike
- eTA issuance failure spike

## Dashboards

- Platform Health
- Application Processing
- Payments
- Security
- Fast Lane
- External Providers
