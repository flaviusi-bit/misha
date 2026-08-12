# MISHA dev watchlist mock

Deterministic HTTP provider used only by `aws-dev` until a real watchlist vendor is selected.

- `POST /screen` requires `X-API-Key`.
- `applicantReference` containing `confirmed` returns `ConfirmedMatch`.
- `applicantReference` containing `potential` returns `PotentialMatch`.
- Other references return `Clear`.
- `GET /health` returns HTTP 200.

The mock never contains a production credential and is not intended for production deployment.
