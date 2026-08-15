# SQS resilience guardrails for the application event queue.
# The queue and redrive policy are defined in main.tf; this file adds
# operational isolation and a visible signal when messages reach the DLQ.

resource "aws_sqs_queue_redrive_allow_policy" "application_events_dlq" {
  queue_url = aws_sqs_queue.application_events_dlq.id

  redrive_allow_policy = jsonencode({
    redrivePermission = "byQueue"
    sourceQueueArns   = [aws_sqs_queue.application_events.arn]
  })
}

resource "aws_cloudwatch_metric_alarm" "application_events_dlq_depth" {
  alarm_name          = "${local.name}-application-events-dlq-depth"
  alarm_description   = "Application event messages reached the DLQ and require investigation."
  namespace           = "AWS/SQS"
  metric_name         = "ApproximateNumberOfMessagesVisible"
  dimensions          = { QueueName = aws_sqs_queue.application_events_dlq.name }
  statistic           = "Maximum"
  period              = 60
  evaluation_periods  = 1
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  depends_on = [terraform_data.worker_iam_policy_propagation]
}
