# Watchlist HTTP Gateway

The ETA workflow now has a vendor-neutral HTTP watchlist adapter. It is deliberately fail-closed: missing configuration, non-HTTPS endpoints, transport failures, invalid JSON, and invalid decisions all produce `Error`, which the policy engine maps to `NotReady`.

## Configuration

```json
"Watchlist": {
  "ProviderName": "provider-name",
  "BaseUrl": "https://provider.example/",
  "Endpoint": "/screen",
  "ApiKey": "set-via-secret-store"
}
```

Do not commit real API keys to source control. Use the deployment secret/configuration mechanism.

## Request contract

The gateway sends `POST {BaseUrl}{Endpoint}` with:

- `documentNumber`
- `issuingCountry`
- `surname`
- `givenNames`
- `dateOfBirth`
- `nationality`
- `expiryDate`

Authentication is sent as `X-API-Key`.

## Response contract

```json
{
  "decision": "Clear | PotentialMatch | ConfirmedMatch",
  "matchReference": "optional-provider-reference",
  "errorMessage": null
}
```

The adapter does not claim to implement any particular watchlist vendor. A real provider must be configured against this contract, or a dedicated vendor adapter should be added if its API differs.
