# Passport Verification HTTP Contract

Misha can delegate passport verification to an external provider without coupling the application or policy layers to a vendor.

## Request

`POST {BaseUrl}{Endpoint}`

Headers:

- `X-API-Key: <configured secret>`
- `Content-Type: application/json`

Body:

```json
{
  "documentNumber": "P1234567",
  "issuingCountry": "ROU",
  "surname": "DOE",
  "givenNames": "JOHN",
  "dateOfBirth": "1990-01-15",
  "nationality": "ROU",
  "expiryDate": "2030-01-15"
}
```

## Response

```json
{
  "decision": "Verified",
  "reference": "provider-reference-123",
  "errorMessage": null
}
```

Allowed decisions are `Verified`, `Rejected`, and `UnableToVerify`.

`NotVerified` and `Error` are treated as provider errors by the gateway. Transport failures, invalid JSON, invalid decisions, missing configuration, and non-success HTTP responses fail closed.

## Configuration

```json
"PassportVerification": {
  "ProviderName": "",
  "BaseUrl": "",
  "Endpoint": "/verify",
  "ApiKey": ""
}
```

`BaseUrl` must use HTTPS. Secrets should be supplied through the deployment secret store/environment configuration rather than committed to source control.
