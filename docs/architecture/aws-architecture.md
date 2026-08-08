# AWS Architecture

## Status

Architecture baseline — 2026-08-08.

## Core platform

```text
Internet
  ↓
CloudFront / WAF
  ↓
ALB
  ↓
ECS Fargate
  ├── MISHA API
  └── MISHA Worker
       ├── PostgreSQL (RDS)
       ├── Redis
       ├── SQS
       └── S3
```

## Network

Production uses a multi-AZ VPC with:

- Public subnets for controlled ingress components.
- Private application subnets for ECS workloads.
- Dedicated private database subnets for PostgreSQL.
- Controlled outbound connectivity.

RDS is never publicly exposed.

## Infrastructure modules

```text
infra/terraform/
├── modules/
│   ├── networking/
│   ├── security/
│   ├── ecs/
│   ├── rds/
│   ├── redis/
│   ├── s3/
│   ├── sqs/
│   ├── iam/
│   ├── observability/
│   └── edge/
└── environments/
    ├── dev/
    ├── staging/
    └── prod/
```

Each environment has isolated infrastructure and state.

## Services

### API

Public application/API entry point behind the ALB.

### Worker

Private asynchronous processing service. It consumes SQS queues and has no public inbound traffic.

### PostgreSQL

Authoritative relational datastore. Production uses encryption, backups, monitoring and Multi-AZ where required.

### Redis

Performance/cache component. Redis is never authoritative for business state.

### SQS

Asynchronous integration boundary. Critical queues use dead-letter queues.

### S3

Private object storage for documents, controlled exports and approved datasets. Public access is blocked.

## Edge

Production target:

```text
Route53 → CloudFront → WAF → ALB → ECS
```

Public endpoints use HTTPS and ACM-managed certificates.

## Secrets and identity

- Secrets Manager for application secrets.
- IAM workload roles rather than embedded AWS credentials.
- GitHub Actions uses OIDC federation.
- Least privilege is mandatory.

## Observability

CloudWatch receives structured logs and infrastructure/application metrics. Trace and correlation identifiers propagate through synchronous and asynchronous operations.

## State rule

Terraform state is remote and locked. State files are never committed to Git.
