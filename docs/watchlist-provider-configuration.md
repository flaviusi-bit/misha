# Watchlist provider configuration

The API can run the parallel watchlist screening endpoint against one or more configured HTTP providers.

## Configuration shape

Use one child section per provider under `Watchlist:Providers`:

```json
{
  "Watchlist": {
    "Providers": {
      "ProviderA": {
        "Name": "provider-a",
        "BaseUrl": "https://provider-a.example/screening",
        "Endpoint": "/screen",
        "ApiKey": "<secret>"
      },
      "ProviderB": {
        "Name": "provider-b",
        "BaseUrl": "https://provider-b.example/api",
        "Endpoint": "/watchlist/screen",
        "ApiKey": "<secret>"
      }
    }
  }
}
```

`Name` is the persisted provider identifier and must be unique case-insensitively. `BaseUrl` must use HTTPS. `Endpoint` is a relative path. `ApiKey` is required for HTTP providers.

Environment-variable configuration follows the normal ASP.NET Core `__` mapping, for example `Watchlist__Providers__ProviderA__ApiKey`.

The existing single-provider configuration (`Watchlist:BaseUrl`, `Watchlist:Endpoint`, `Watchlist:ApiKey`, `Watchlist:ProviderName`) remains supported for backward compatibility. The development `dev-mock` provider remains available through that legacy configuration path.

## Resilience isolation

Each configured provider gets its own retry/circuit-breaker pipeline. A failing provider therefore does not open the circuit for another provider. Transient HTTP 408, 429 and 5xx responses are retried twice with exponential backoff and jitter; sustained transient failures can open that provider's circuit. Provider failures remain fail-closed as `Error` decisions.

## Secrets

Do not commit API keys to source control. In ECS, inject provider API keys through the existing secrets/configuration mechanism and expose them as environment variables using the `Watchlist__Providers__<ProviderKey>__ApiKey` naming convention.
