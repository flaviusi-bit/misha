# Security Architecture

## Security model

MISHA uses identity, authentication, authorization, least privilege, encryption, audit and monitoring as baseline controls.

## Trust boundaries

```text
Internet → WAF/ALB → ECS → internal services/data/providers
```

Every boundary is treated as untrusted until authenticated and authorized.

## Roles

Initial roles:

- Traveller
- Officer
- Supervisor
- Administrator
- SecurityAdministrator
- PolicyAdministrator
- DeviceAdministrator
- Auditor

Authorization is enforced server-side. UI visibility is never an authorization control.

## Sensitive data

Passport identifiers, biometric material, watchlist results and payment information are highly sensitive. Raw values must not be written to ordinary application logs.

## Secrets

Secrets must not be stored in Git, Docker images, source code or plaintext Terraform variables. AWS Secrets Manager is the target secret store.

## API security

- Authentication
- Server-side authorization
- Input validation
- Payload limits
- Rate limiting
- Security headers
- Audit logging

## Integration security

Outbound provider calls use allow-listed endpoints. User-controlled URLs must never be fetched directly by backend infrastructure.

## File uploads

Uploads require type validation, size limits, content validation, malware scanning where required, hashing and controlled S3 storage.

## IAM

Workloads receive scoped IAM roles. Human production access is restricted and audited.

## Audit

Security-sensitive operations create auditable records, including policy changes, decision overrides, eTA issuance/revocation, privileged access and bulk exports.

## Required security validation before production

- Threat modeling
- Dependency scanning
- Container scanning
- IAM review
- API authorization tests
- IDOR testing
- Injection testing
- File upload abuse testing
- Authentication bypass testing
- Penetration testing
- Data protection review
