# Test Strategy

## Test pyramid

```text
          E2E
        /     \
  Contract   Integration
      \       /
        Unit
```

Unit tests are the largest layer. E2E tests cover only critical business journeys.

## Unit tests

Cover domain aggregates, state transitions, policy rules, validators, mappers and error handling.

## Integration tests

Cover PostgreSQL, Redis, S3, SQS and provider adapters.

## Contract tests

Validate OpenAPI and external provider contracts for passport, watchlist, biometrics and payment integrations.

## Critical E2E flow

```text
Create application
 → Submit
 → Checks
 → Decision
 → Payment
 → eTA
 → Verification
```

## Security tests

Test unauthorized access, privilege escalation, IDOR, rate limiting, injection, file upload abuse and authentication bypass.

## Concurrency tests

Test duplicate submission, duplicate payment callbacks, duplicate issuance and concurrent review claiming.

## Resilience tests

Test provider timeout/outage, SQS redelivery, worker crash, database failover, Redis failure and S3 failure.

## Acceptance scenarios

1. Valid application completes the expected lifecycle.
2. Duplicate submission does not create duplicate processing.
3. Concurrent submissions use optimistic concurrency/idempotency controls.
4. Watchlist potential match routes to manual review when policy requires it.
5. Provider timeout does not become an implicit CLEAR result.
6. Payment callback replay cannot duplicate financial state.
7. Approved and paid application produces exactly one eTA.
8. Revoked eTA is no longer valid for verification.
9. Unauthorized users cannot revoke eTAs.
10. Historical decisions remain reproducible after policy changes.

Production data is prohibited in automated development/test environments unless an approved anonymization process exists.
