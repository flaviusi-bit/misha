# MVP Readiness Review

## Architecture status

Architecture is sufficiently defined to continue implementation.

Architecture readiness does not mean production readiness.

## MVP scope

1. Traveller
2. Application
3. Document upload
4. Passport verification
5. Watchlist screening
6. Policy evaluation
7. Payment
8. Decision
9. eTA issuance
10. eTA verification
11. Notifications
12. Audit
13. Admin/manual review
14. AWS deployment
15. CI/CD

Fast Lane is a controlled accelerated/offline capability and should not block the basic ETA lifecycle.

## Critical path

```text
Authentication
 → Application
 → Documents
 → Passport
 → Watchlist
 → Policy
 → Decision
 → Payment
 → eTA
 → Verification
```

## Current implementation status

The repository currently has implemented slices for the critical path through eTA verification, plus payment reconciliation, cryptographic eTA trust, audit history, and the first manual-review queue implementation.

Document capabilities now implemented and deployed include traveller profile, pre-signed upload/download transfer, a content-safety validation boundary, and explicit document deletion with S3 and PostgreSQL cleanup.

Manual review now supports:

- automatic queue creation when the decision engine returns `ManualReview`
- one open case per application
- authenticated queue inspection
- reviewer assignment
- explicit approve/refuse resolution with a mandatory reason
- application/review persistence in one save operation

The remaining production gates below are still open.

## External blockers

Production requires confirmed providers/configuration for:

- Identity provider
- Payment provider
- Watchlist provider/source
- Passport verification provider
- Biometric provider where required
- Jurisdiction-specific policy
- Data retention requirements
- Production AWS account structure
- DNS/domain and TLS

## Production gates

Do not release with unresolved:

- Critical security failure
- Critical data-loss risk
- Payment integrity failure
- Identity bypass
- Watchlist bypass
- Unsafe eTA issuance
- Broken audit trail
- Uncontrolled administrator access

## Definition of done

For each critical capability, progress must be visible through:

```text
SPECIFIED
 → IMPLEMENTED
 → TESTED
 → DEPLOYED
 → PRODUCTION-VALIDATED
```

Documentation alone is not implementation evidence.

## Recommended implementation order

1. Repository and compileable solution
2. Database and application API
3. Authentication
4. Documents and S3
5. Passport adapter
6. Watchlist adapter
7. Policy engine
8. Decision workflow
9. Payment
10. eTA issuance
11. Verification
12. Manual review
13. Fast Lane
14. Notifications
15. AWS and CI/CD
16. Security and performance validation
17. Production readiness
