# Terraform must be able to read the existing dashboard during refresh/plan.
# Keep the dashboard ARN scoped to this environment.
resource "aws_iam_role_policy" "github_actions_cloudwatch_dashboard_refresh" {
  name = "${local.name}-github-actions-cloudwatch-dashboard-refresh"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "cloudwatch:GetDashboard"
      ]
      Resource = "arn:aws:cloudwatch:*:${data.aws_caller_identity.current.account_id}:dashboard/${local.name}-*"
    }]
  })
}
