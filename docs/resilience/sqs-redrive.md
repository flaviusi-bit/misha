# SQS retry and DLQ validation

The application event queue uses AWS-managed redrive semantics. The source queue is configured with a `maxReceiveCount` of 5 and a dedicated DLQ. The consumer contract intentionally deletes a message only after successful processing; a processing failure leaves the message undeleted so SQS can redeliver it.

## Infrastructure guardrails

`infrastructure/terraform/sqs-resilience.tf` restricts the DLQ redrive source to the application event queue and creates a CloudWatch alarm whenever messages become visible in the DLQ.

## Runtime validation

The repository includes `scripts/validate-sqs-redrive.sh`. It deliberately sends one uniquely marked test message, receives it without deleting it, and uses `VisibilityTimeout=0` to force immediate redelivery. This validates the AWS redrive counter without requiring a worker process to crash.

Run only against a non-production validation queue unless an explicit production test has been approved:

```bash
CONFIRM_DLQ_TEST=I_UNDERSTAND \
  bash scripts/validate-sqs-redrive.sh \
  "$APPLICATION_EVENTS_QUEUE_URL" \
  "$APPLICATION_EVENTS_DLQ_URL"
```

A successful run prints `PASS: validation message reached the DLQ.` and removes the validation message from the DLQ.

The test is intentionally not part of the normal CI pipeline because it mutates live SQS state and is therefore an operational validation, not a deterministic build test.
