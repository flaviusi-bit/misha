# MISHA

MISHA is the working repository for the ETA / border-management platform.

## Engineering baseline

The architecture and delivery baseline defined during the initial design phase is now stored in this repository under `docs/`.

### Technology decisions

- **Cloud:** AWS
- **Database:** PostgreSQL
- **Backend:** .NET / C#
- **Compute:** ECS Fargate
- **Messaging:** Amazon SQS
- **Object storage:** Amazon S3
- **Cache:** managed Redis
- **Infrastructure as Code:** Terraform
- **CI/CD:** GitHub Actions + AWS OIDC

## Repository structure

```text
Misha/
├── docs/
│   ├── architecture/
│   │   └── aws-architecture.md
│   ├── security/
│   │   └── security-architecture.md
│   ├── observability/
│   │   └── observability.md
│   ├── testing/
│   │   └── test-strategy.md
│   └── release/
│       └── mvp-readiness.md
├── src/
├── tests/
├── infra/
│   └── terraform/
└── .github/
    └── workflows/
```

## MVP critical path

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

Fast Lane is a controlled accelerated/offline verification capability around the issued eTA.

## Engineering state model

Every capability is tracked separately as:

1. **SPECIFIED**
2. **IMPLEMENTED**
3. **TESTED**
4. **DEPLOYED**
5. **PRODUCTION-VALIDATED**

Architecture documentation is not treated as implementation evidence.

## Current status

**Architecture:** baseline established  
**Implementation:** starting from repository bootstrap  
**Production readiness:** not yet claimed

## Documentation

- [AWS Architecture](docs/architecture/aws-architecture.md)
- [Security Architecture](docs/security/security-architecture.md)
- [Observability](docs/observability/observability.md)
- [Test Strategy](docs/testing/test-strategy.md)
- [MVP Readiness](docs/release/mvp-readiness.md)
