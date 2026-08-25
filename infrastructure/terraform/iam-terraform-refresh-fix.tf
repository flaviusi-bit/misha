# IAM ListInstanceProfilesForRole is a List action and must be granted on
# the wildcard resource. Terraform needs it while reading/deleting IAM roles.
# iam:CreatePolicy is required because the backup-restore recovery refactor
# creates a managed policy before it can attach it to the deploy role.
# iam:TagPolicy and iam:ListPolicyTags are required because the AWS provider
# sends provider default_tags with the managed policy CreatePolicy request.
resource "aws_iam_role_policy" "github_actions_iam_refresh" {
  name = "${local.name}-github-actions-iam-refresh"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "iam:ListInstanceProfilesForRole",
          "iam:CreatePolicy"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "iam:ListPolicyTags",
          "iam:TagPolicy",
          "iam:UntagPolicy"
        ]
        Resource = "arn:aws:iam::576984879588:policy/${local.name}-*"
      }
    ]
  })
}
