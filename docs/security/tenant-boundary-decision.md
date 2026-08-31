# Tenant boundary decision

## Decision

MISHA uses an explicit server-side mapping between an authenticated Cognito application client and a MISHA tenant. Cognito `client_id` is the lookup key, not the tenant identifier itself.

## Authorization contract

1. Authentication establishes the principal.
2. The server reads the authenticated `client_id`.
3. The server resolves that client to a tenant using trusted server-side configuration/data.
4. Request payloads and URL parameters cannot select the tenant.
5. Application and applicant resources are authorized against the resolved tenant.
6. `misha-admin` is the explicit cross-tenant exception.
7. Child resources are reachable only through an authorized parent application boundary.

## Current deployment implication

The current Terraform baseline has one Cognito user-pool client and no tenant registry. The implementation must therefore introduce the mapping deliberately rather than equating `client_id` with `TenantId`.

## Security invariants

- No client-controlled tenant ID is trusted.
- Missing client-to-tenant mapping fails closed.
- Cross-tenant resource existence is not disclosed.
- CI/CD permissions and deployment behavior remain unchanged.
