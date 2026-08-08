# MISHA

MISHA is the working repository for the ETA / border-management platform.

## Engineering baseline

The architecture and delivery baseline defined during the initial design phase is stored in this repository under `docs/`.

### Technology decisions

- **Cloud:** AWS
- **Database:** PostgreSQL
- **Backend:** .NET 10 / C#
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
│   ├── security/
│   ├── observability/
│   ├── testing/
│   └── release/
├── src/
│   ├── Misha.Domain/
│   ├── Misha.Application/
│   ├── Misha.Infrastructure/
│   ├── Misha.Api/
│   └── Misha.Worker/
├── tests/
│   └── Misha.Domain.Tests/
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
**Repository bootstrap:** IMPLEMENTED  
**Domain application slice:** IMPLEMENTED  
**Automated tests:** CREATED; CI validation pending  
**AWS deployment:** NOT IMPLEMENTED  
**Production readiness:** NOT CLAIMED

## Local development

Start PostgreSQL:

```bash
docker compose up -d postgres
```

Provide the connection string through the environment rather than committing credentials.

Build and test:

```bash
dotnet restore Misha.slnx
dotnet build Misha.slnx
dotnet test Misha.slnx
```

## Documentation

- [AWS Architecture](docs/architecture/aws-architecture.md)
- [Security Architecture](docs/security/security-architecture.md)
- [Observability](docs/observability/observability.md)
- [Test Strategy](docs/testing/test-strategy.md)
- [MVP Readiness](docs/release/mvp-readiness.md)
