# Parallel watchlist screening

The application exposes `POST /applications/{id}/watchlist/screen/parallel` for provider fan-out.

The endpoint invokes every registered `IWatchlistProvider` concurrently and persists one `WatchlistCheck` per provider. Each provider is isolated with a 10-second timeout, so a slow or failed provider does not prevent other providers from completing.

Decision precedence is:

1. `ConfirmedMatch`
2. `PotentialMatch`
3. `Error`
4. `Clear`

A provider timeout is recorded as `Error` with an explicit timeout message. Provider exceptions are also recorded as `Error`; they are not allowed to turn an otherwise incomplete screening into `Clear`.

The aggregate also exposes `HasConflictingResults` when at least one provider returns `Clear` while another returns `PotentialMatch` or `ConfirmedMatch`. This preserves the strongest decision while making cross-provider disagreement explicit for downstream decisioning and audit.

Each persisted `WatchlistCheck` retains the provider name, decision, match reference when supplied, error message when applicable, creation timestamp, and completion timestamp. The orchestration result additionally exposes per-provider duration and timeout status for operational/audit consumers.

This endpoint is intentionally separate from the existing single-provider screening endpoint while additional real providers are being integrated. No vendor-specific behavior is introduced by the orchestration layer.
