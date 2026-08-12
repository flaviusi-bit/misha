# Watchlist runtime configuration

The MISHA ECS task receives the watchlist provider configuration from AWS Secrets Manager. No provider API key is stored in Git or in the ECS task definition.

## Secret

Terraform creates:

`misha-<environment>/application`

The secret must contain JSON fields:

```json
{
  "WatchlistProviderName": "provider-name",
  "WatchlistBaseUrl": "https://provider.example/",
  "WatchlistEndpoint": "/screen",
  "WatchlistApiKey": "replace-with-real-secret"
}
```

`WatchlistBaseUrl` must use HTTPS. `WatchlistEndpoint` is a relative path. The application rejects missing/invalid provider configuration and fails closed.

## ECS mapping

The task definition maps the secret JSON keys to .NET configuration environment variables:

- `WatchlistProviderName` -> `Watchlist__ProviderName`
- `WatchlistBaseUrl` -> `Watchlist__BaseUrl`
- `WatchlistEndpoint` -> `Watchlist__Endpoint`
- `WatchlistApiKey` -> `Watchlist__ApiKey`

The ECS execution role is granted `secretsmanager:GetSecretValue` only for the application secret and the RDS-managed master secret.

## Provider contract

The current adapter sends a `POST` request with passport identity fields and `X-API-Key` authentication. The provider response must contain a supported decision: `Clear`, `PotentialMatch`, or `ConfirmedMatch`, with optional match/error information.

See `docs/watchlist-gateway.md` for the full vendor-neutral HTTP contract.

## Deployment note

Terraform creates the secret container but intentionally does not populate a real provider credential. Before starting an ECS deployment that uses watchlist screening, populate the secret in the target AWS account through the approved secret-management process.

Never commit a real API key to Git, Terraform variables, workflow files, or application settings.
