# Terraform apply needs these APIs when creating/updating the observability dashboard
# and when correcting a private subnet's route-table association.
resource "aws_iam_role_policy" "github_actions_observability_route_refresh" {
  name = "${local.name}-github-actions-observability-route-refresh"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "cloudwatch:GetDashboard",
          "cloudwatch:PutDashboard",
          "cloudwatch:DeleteDashboard"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "ec2:ReplaceRouteTableAssociation"
        ]
        Resource = "*"
      }
    ]
  })
}
