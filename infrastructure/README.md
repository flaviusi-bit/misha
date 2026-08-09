# AWS Infrastructure

This directory contains the Terraform foundation for deploying MISHA to AWS.

## Target architecture

- ECS Fargate for the API and asynchronous workers
- RDS PostgreSQL for durable state
- S3 for document storage
- SQS for asynchronous processing
- CloudWatch for logs and alarms
- IAM task/execution roles with least-privilege policies
- AWS Secrets Manager for runtime secrets

The first slice intentionally creates the shared AWS boundaries without embedding provider credentials or applicant data in Terraform state by default.

Production deployment still requires environment-specific networking, DNS/TLS, secret values, backup policy, alert thresholds and operational validation.
