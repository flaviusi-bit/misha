# Manual Review Operations

Manual review is the human-in-the-loop path for applications that the policy/decision layer cannot safely approve or refuse automatically.

## Flow

```text
Policy / Decision
      |
      | ManualReview
      v
Pending case
      |
      | assign
      v
InProgress
      |
      | resolve
      +------> Approved application
      |
      +------> Refused application
```

A decision returning `ManualReview` creates at most one open case for the application. The case stores the trigger and review reason but no verification token or applicant secret.

## API

All manual-review endpoints require authentication.

- `GET /admin/manual-reviews` — list open cases oldest first
- `GET /admin/manual-reviews/{id}` — inspect one case
- `POST /admin/manual-reviews/{id}/assign` — assign the authenticated reviewer
- `POST /admin/manual-reviews/{id}/resolve` — resolve as `Approve` or `Refuse`

The reviewer identity is taken from the authenticated `sub` or `NameIdentifier` claim; it is never supplied by the request body.

A resolution must include a non-empty reason. Resolving an application and its review case is persisted in one `DbContext.SaveChangesAsync` operation.

## Security boundary

The queue is authenticated and exposes only application identifiers and review metadata. It does not expose passport numbers, verification tokens, token hashes, payment credentials, or applicant secrets.

Role/permission policy for dedicated reviewer and administrator roles remains a deployment-specific authorization concern and should be enforced before production exposure of these endpoints.
