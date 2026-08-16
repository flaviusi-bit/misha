# Terraform creates the CloudWatch dashboard and may re-associate private
# route tables during the resilience rollout. These permissions are scoped to
# the GitHub Actions deployment role used for the dev environment.

resource "aws_iam_role_policy" "github_actions_observability_network" {
  name = "${local.name}-github-actions-observability-network"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["cloudwatch:PutDashboard"]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = ["ec2:ReplaceRouteTableAssociation"]
        Resource = "*"
      }
    ]
  })

  depends_on = [terraform_data.iam_policy_propagation]
}
