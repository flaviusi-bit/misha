#!/usr/bin/env bash
set -euo pipefail

# Intentionally forces one test message through SQS receive attempts without
# deleting it. This validates AWS-managed redrive without requiring a worker.
# Usage:
#   CONFIRM_DLQ_TEST=I_UNDERSTAND ./scripts/validate-sqs-redrive.sh <source-url> <dlq-url>

if [[ "${CONFIRM_DLQ_TEST:-}" != "I_UNDERSTAND" ]]; then
  echo "Refusing to run. Set CONFIRM_DLQ_TEST=I_UNDERSTAND explicitly."
  exit 2
fi

SOURCE_QUEUE_URL="${1:?source queue URL required}"
DLQ_URL="${2:?DLQ URL required}"
MARKER="misha-dlq-validation-$(date +%s)-$$"
MAX_ATTEMPTS="${MAX_RECEIVE_ATTEMPTS:-8}"

aws sqs send-message \
  --queue-url "$SOURCE_QUEUE_URL" \
  --message-body "{\"validation\":\"$MARKER\"}" \
  >/dev/null

echo "Sent validation message: $MARKER"

for ((attempt=1; attempt<=MAX_ATTEMPTS; attempt++)); do
  receipt="$(aws sqs receive-message \
    --queue-url "$SOURCE_QUEUE_URL" \
    --max-number-of-messages 1 \
    --visibility-timeout 0 \
    --wait-time-seconds 2 \
    --query 'Messages[0].ReceiptHandle' \
    --output text)"

  if [[ "$receipt" != "None" && -n "$receipt" ]]; then
    echo "Source receive attempt $attempt succeeded."
  else
    echo "No source message on attempt $attempt; checking DLQ."
  fi

  body="$(aws sqs receive-message \
    --queue-url "$DLQ_URL" \
    --max-number-of-messages 1 \
    --visibility-timeout 0 \
    --wait-time-seconds 1 \
    --query 'Messages[0].Body' \
    --output text)"

  if [[ "$body" == *"$MARKER"* ]]; then
    echo "PASS: validation message reached the DLQ."
    dlq_receipt="$(aws sqs receive-message \
      --queue-url "$DLQ_URL" \
      --max-number-of-messages 1 \
      --visibility-timeout 0 \
      --wait-time-seconds 1 \
      --query 'Messages[0].ReceiptHandle' \
      --output text)"
    if [[ "$dlq_receipt" != "None" && -n "$dlq_receipt" ]]; then
      aws sqs delete-message --queue-url "$DLQ_URL" --receipt-handle "$dlq_receipt"
    fi
    exit 0
  fi
done

echo "FAIL: validation message did not reach the DLQ within $MAX_ATTEMPTS receive attempts."
exit 1
