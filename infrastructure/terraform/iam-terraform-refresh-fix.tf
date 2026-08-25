# IAM ListInstanceProfilesForRole is a List action and must be granted on
# the wildcard resource. Terraform needs it while reading/deleting IAM roles.
# iam:CreatePolicy is temporarily required because the backup-restore recovery
# refactor creates a managed policy before it can attach it to the deploy role.
resource "aws_iam_role_policy" "github_actions_iam_refresh" {
  name = "${local.name}-github-actions-iam-refresh"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "iam:ListInstanceProfilesForRole",
        "iam:CreatePolicy"
      ]
      Resource = "*"
    }]
  })
}
