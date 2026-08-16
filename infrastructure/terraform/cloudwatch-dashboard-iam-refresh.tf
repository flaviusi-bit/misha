# Terraform must be able to read the existing dashboard during refresh/plan.
# CloudWatch GetDashboard requires dashboard-level access; scope is enforced
# by the dashboard name used by this environment.
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
      Resource = "*"
    }]
  })
}
