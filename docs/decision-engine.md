# Decision Engine

The decision engine is the controlled bridge between eligibility policy and application state transitions.

## Rules

- `Eligible` is the only policy result that can produce `Approve`.
- `Ineligible` produces `Refuse` using the policy reasons.
- `ManualReview` never changes application status.
- `NotReady` never changes application status.
- The domain aggregate remains the final guard: approval/refusal is only valid while the application is `Processing`.
- Every decision attempt is recorded in the append-only `decision_audits` table with the policy version, actor, outcome, and reasons.
- Concurrent decisions are protected by PostgreSQL optimistic concurrency via the application's `xmin` row-version token.

## API

- `POST /applications/{id}/decision` evaluates policy and applies the controlled outcome.
- `GET /applications/{id}/decision/audit` returns the decision history newest-first.

The engine intentionally does not add jurisdiction-specific eligibility rules. Those belong in policy modules/configuration and must remain explicit and testable.
