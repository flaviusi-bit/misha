# IAM ListInstanceProfilesForRole is a List action and must be granted on
# the wildcard resource. Terraform needs it while reading/deleting IAM roles.
# Managed-policy lifecycle permissions are required because the backup-restore
# recovery refactor creates and then refreshes a customer-managed policy.
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
          "iam:GetPolicy",
          "iam:GetPolicyVersion",
          "iam:ListPolicyVersions",
          "iam:CreatePolicyVersion",
          "iam:DeletePolicyVersion",
          "iam:ListPolicyTags",
          "iam:TagPolicy",
          "iam:UntagPolicy",
          "iam:DeletePolicy"
        ]
        Resource = "arn:aws:iam::576984879588:policy/${local.name}-*"
      }
    ]
  })
}
