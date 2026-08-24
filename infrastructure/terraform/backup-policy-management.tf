resource "aws_iam_role_policy" "github_actions_backup_restore_policy_management" {
  name = "${local.name}-backup-restore-policy-management"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "iam:CreatePolicy",
        "iam:CreatePolicyVersion",
        "iam:DeletePolicy",
        "iam:DeletePolicyVersion",
        "iam:GetPolicy",
        "iam:GetPolicyVersion",
        "iam:ListPolicyVersions",
        "iam:SetDefaultPolicyVersion"
      ]
      Resource = "arn:aws:iam::576984879588:policy/${local.name}-github-actions-backup-restore-testing"
    }]
  })
}
