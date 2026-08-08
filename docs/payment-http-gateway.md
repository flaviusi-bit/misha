# Payment HTTP gateway contract

Misha can integrate a real payment service through the `IPaymentProvider` boundary without coupling the domain to a vendor SDK.

## Configuration

Set these configuration values through environment variables or another secure configuration source:

- `Payment:ProviderName` — provider identifier stored with the payment.
- `Payment:BaseUrl` — **HTTPS** base URL of the payment service.
- `Payment:Endpoint` — absolute-path endpoint relative to `BaseUrl`; defaults to `/payments`.
- `Payment:ApiKey` — API key sent as `X-API-Key`.

The API key must not be committed to source control.

## Request

`POST {BaseUrl}{Endpoint}`

```json
{
  "paymentId": "uuid",
  "applicationId": "uuid",
  "amountMinor": 12500,
  "currency": "EUR"
}
```

`amountMinor` is the integer amount in the currency's minor unit (for example, cents for EUR).

## Response

```json
{
  "status": "RequiresAction",
  "reference": "provider-payment-123",
  "actionUrl": "https://payments.example/checkout/provider-payment-123",
  "errorMessage": null
}
```

Supported statuses are `Pending`, `RequiresAction`, `Paid`, and `Failed`. `Cancelled` is not accepted from the provider during payment creation.

When `RequiresAction` is returned, `actionUrl` must be an absolute HTTPS URL. Misha exposes it to the authenticated client so the applicant can complete the payment step.

## Flow

Payment is collected after application submission/processing and **before the final eligibility decision**. A successful payment does not imply approval. The subsequent decision may still be `Approved`, `Refused`, or another controlled outcome.

The gateway is deliberately vendor-neutral. A provider-specific adapter can implement this contract without changing the domain or application layers.
