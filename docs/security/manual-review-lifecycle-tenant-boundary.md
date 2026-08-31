# Tenant Boundary Hardening: Manual Reviews and Lifecycle Audits

## Scope

Manual review cases and application lifecycle audits are tenant-owned data because both are associated with an application.

Repositories exposing application-scoped data must enforce the same tenant boundary used by application, payment, passport, watchlist, and decision-audit repositories.

## Required behavior

- Non-admin callers may access only records whose parent application belongs to their resolved tenant.
- Admin callers retain cross-tenant access.
- Missing tenant context must fail closed for non-admin callers.
- Public ETA verification remains a separate token-based use case and is not covered by this rule.

## Implementation note

The repository queries should enforce ownership through `ApplicationId -> Applications.TenantId`, avoiding redundant `TenantId` columns on child tables.

## Validation

Cross-tenant integration tests must cover get-by-id, application-scoped lookups, and collection queries for manual reviews and lifecycle audits.
