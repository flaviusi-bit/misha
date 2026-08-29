resource "aws_iam_policy" "operational_alerting" {
  name        = "${local.name}-operational-alerting"
  description = "Minimal permissions required by GitHub Actions to manage operational SNS and EventBridge resources."

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sns:CreateTopic",
          "sns:DeleteTopic",
          "sns:GetTopicAttributes",
          "sns:SetTopicAttributes",
          "sns:ListTagsForResource",
          "sns:TagResource",
          "sns:UntagResource",
          "sns:Subscribe",
          "sns:Unsubscribe",
          "sns:ListSubscriptionsByTopic"
        ]
        Resource = "arn:aws:sns:${var.aws_region}:576984879588:${local.name}-*"
      },
      {
        Effect = "Allow"
        Action = [
          "events:PutRule",
          "events:DeleteRule",
          "events:DescribeRule",
          "events:EnableRule",
          "events:DisableRule",
          "events:ListTagsForResource",
          "events:TagResource",
          "events:UntagResource",
          "events:PutTargets",
          "events:RemoveTargets",
          "events:ListTargetsByRule"
        ]
        Resource = [
          "arn:aws:events:${var.aws_region}:576984879588:rule/${local.name}-*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "github_actions_operational_alerting" {
  role       = aws_iam_role.github_actions_deploy.name
  policy_arn = aws_iam_policy.operational_alerting.arn
}

resource "aws_sns_topic" "operational_alerts" {
  name       = "${local.name}-operational-alerts"
  depends_on = [aws_iam_role_policy_attachment.github_actions_operational_alerting]
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

  depends_on = [aws_iam_role_policy_attachment.github_actions_operational_alerting]

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
