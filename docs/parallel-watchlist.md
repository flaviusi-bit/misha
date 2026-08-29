# Parallel watchlist screening

`POST /applications/{id}/watchlist/screen/parallel` fans out screening to every registered `IWatchlistProvider`, persists one `WatchlistCheck` per provider, and returns the aggregate decision.

Decision precedence: ConfirmedMatch, PotentialMatch, Error, Clear.

Provider failures are isolated so the remaining providers can complete. The endpoint is separate from the existing single-provider endpoint while real external providers are added.