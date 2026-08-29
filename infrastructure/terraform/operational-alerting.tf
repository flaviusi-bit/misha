resource "aws_sns_topic" "operational_alerts" {
  name = "${local.name}-operational-alerts"

  depends_on = [aws_iam_role_policy_attachment.github_actions_sns]
}

resource "aws_sns_topic_policy" "operational_alerts" {
  arn    = aws_sns_topic.operational_alerts.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowEventBridgePublish"
      Effect    = "Allow"
      Principal = { Service = "events.amazonaws.com" }
      Action    = "sns:Publish"
      Resource  = aws_sns_topic.operational_alerts.arn
    }]
  })
}

resource "aws_sns_topic_subscription" "operational_alerts_email" {
  count     = var.operational_alert_email == "" ? 0 : 1
  topic_arn = aws_sns_topic.operational_alerts.arn
  protocol  = "email"
  endpoint  = var.operational_alert_email
}

resource "aws_cloudwatch_event_rule" "operational_alarm_state_change" {
  name        = "${local.name}-operational-alarm-state-change"
  description = "Route CloudWatch alarm state changes to the operational alert topic."

  depends_on = [aws_iam_role_policy_attachment.github_actions_eventbridge]

  event_pattern = jsonencode({
    source      = ["aws.cloudwatch"]
    detail-type = ["CloudWatch Alarm State Change"]
    detail = {
      state = {
        value = ["ALARM"]
      }
    }
  })
}

resource "aws_cloudwatch_event_target" "operational_alarm_state_change" {
  rule = aws_cloudwatch_event_rule.operational_alarm_state_change.name
  arn  = aws_sns_topic.operational_alerts.arn
}

output "operational_alerts_topic_arn" {
  description = "SNS topic receiving production alarm notifications."
  value       = aws_sns_topic.operational_alerts.arn
}
