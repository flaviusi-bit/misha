# Audit identity context

MISHA uses the Cognito access-token subject (`sub`) as the stable audit actor reference.

## Rules

- Audit identity is derived only after authentication and authorization.
- The immutable `sub` claim is preferred over the mutable `username` claim.
- `client_id` is captured when present for machine/client attribution.
- Sensitive applicant/profile values are never written to the audit log.
- Applicant profile updates emit a structured security-audit event containing the applicant id, actor subject, client id and resulting completion state.
- Application lifecycle audits continue to use the authenticated actor reference.

The API centralizes claim extraction in `AuditIdentityContext` so security-sensitive endpoints do not each implement their own claim-selection logic.
