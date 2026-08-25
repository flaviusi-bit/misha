resource "aws_iam_policy" "github_actions_backup_restore_testing" {
  name = "${local.name}-github-actions-backup-restore-testing"

  depends_on = [aws_iam_role_policy.github_actions_iam_refresh]

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "backup:CreateRestoreTestingPlan",
          "backup:DeleteRestoreTestingPlan",
          "backup:GetRestoreTestingPlan",
          "backup:ListRestoreTestingPlans",
          "backup:UpdateRestoreTestingPlan",
          "backup:CreateRestoreTestingSelection",
          "backup:DeleteRestoreTestingSelection",
          "backup:GetRestoreTestingSelection",
          "backup:ListRestoreTestingSelections",
          "backup:UpdateRestoreTestingSelection",
          "backup:ListRestoreJobs"
        ]
        Resource = "*"
      },
      {
        Effect   = "Allow"
        Action   = ["iam:CreateServiceLinkedRole"]
        Resource = "*"
        Condition = {
          StringEquals = {
            "iam:AWSServiceName" = "restore-testing.backup.amazonaws.com"
          }
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "github_actions_backup_restore_testing" {
  role       = aws_iam_role.github_actions_deploy.name
  policy_arn = aws_iam_policy.github_actions_backup_restore_testing.arn
}
