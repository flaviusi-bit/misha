# Authentication and Authorization Model

## Trust boundary

MISHA APIs accept **Cognito access tokens only**. ID tokens are not API credentials.

The API validates:

- issuer and signing keys from the configured Cognito authority;
- token lifetime;
- `token_use=access`;
- API scopes from the `scope` claim;
- Cognito group membership from `cognito:groups`.

## Authorization model

Authorization is deny-by-default and requires both a required scope and an allowed operational role.

| Policy | Scope | Allowed roles |
|---|---|---|
| `api.read` | `read` | admin, operator, reviewer, auditor |
| `api.write` | `write` | admin, operator |
| `decision.read` | `decision.read` | admin, operator, reviewer, auditor |
| `decision.write` | `decision.write` | admin, operator |
| `review.read` | `review.read` | admin, reviewer, auditor |
| `review.write` | `review.write` | admin, reviewer |

A valid scope without the required group is insufficient.

## Role intent

- **admin** — full administrative and operational authority.
- **operator** — application processing and decision authority.
- **reviewer** — manual-review queue access without decision-write authority.
- **auditor** — read-only operational and audit access.

## Security invariants

1. No endpoint is public unless explicitly designed as a health/availability endpoint.
2. API write access cannot be obtained by possessing a write scope alone.
3. Decision write access cannot be obtained by a reviewer or auditor.
4. Review write access cannot be obtained by an operator or auditor.
5. An ID token cannot be substituted for an access token.
6. Adding a new endpoint requires an explicit authorization policy decision.

These invariants are covered by automated authorization tests and should remain part of the security regression suite.
