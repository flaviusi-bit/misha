# Parallel watchlist screening

The application exposes `POST /applications/{id}/watchlist/screen/parallel` for provider fan-out.

The endpoint invokes every registered `IWatchlistProvider` concurrently, persists one `WatchlistCheck` per provider, and returns an aggregate decision.

Decision precedence is:

1. `ConfirmedMatch`
2. `PotentialMatch`
3. `Error`
4. `Clear`

Provider failures are isolated so one provider can fail without preventing the remaining providers from completing. This endpoint is intentionally separate from the existing single-provider screening endpoint while additional real providers are being integrated.
