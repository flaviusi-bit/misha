# Observability and Runtime Health

## Health endpoints

The API exposes two health endpoints:

- `/health/live` — process/liveness only. It does not depend on external services.
- `/health/ready` — readiness, including the PostgreSQL dependency.

The ECS target group uses `/health/ready`, so an instance is removed from service when the application cannot reach its database.

## Logs

ECS sends application logs to CloudWatch Logs under `/ecs/<environment>/api` with a 30-day retention period.

Logs must not contain passport numbers, document contents, access tokens, API keys, passwords, or other secrets. Prefer stable application IDs and provider reference IDs for correlation.

## Container and deployment signals

ECS Container Insights is enabled. Production deployments use the ECS deployment circuit breaker with automatic rollback.

## Incident response baseline

When an ECS deployment becomes unhealthy:

1. Check target-group health and `/health/ready` failures.
2. Inspect the API CloudWatch log stream for the failing task.
3. Check ECS deployment events and circuit-breaker state.
4. Verify RDS connectivity and health.
5. If required, use the ECS deployment rollback and investigate the failed image/configuration before retrying.
