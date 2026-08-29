resource "aws_iam_role_policy" "github_actions_operational_alerting" {
  name = "${local.name}-github-actions-operational-alerting"
  role = aws_iam_role.github_actions_deploy.id

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
        Resource = "arn:aws:sns:${var.aws_region}:576984879588:${local.name}-operational-alerts"
      },
      {
        Effect = "Allow"
        Action = [
          "events:PutRule",
          "events:DeleteRule",
          "events:DescribeRule",
          "events:TagResource",
          "events:UntagResource",
          "events:PutTargets",
          "events:RemoveTargets"
        ]
        Resource = "arn:aws:events:${var.aws_region}:576984879588:rule/${local.name}-operational-alarm-state-change"
      }
    ]
  })
}
