resource "aws_iam_role_policy" "github_actions_backup_restore_testing" {
  name = "${local.name}-github-actions-backup-restore-testing"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = [
          "backup:CreateRestoreTestingPlan",
          "backup:DeleteRestoreTestingPlan",
          "backup:GetRestoreTestingPlan",
          "backup:ListRestoreTestingPlans",
          "backup:UpdateRestoreTestingPlan",
          "backup:CreateRestoreTestingSelection",
          "backup:DeleteRestoreTestingSelection",
          "backup:GetRestoreTestingSelection",
          "backup:ListRestoreTestingSelections",
          "backup:UpdateRestoreTestingSelection"
        ]
        Resource = "*"
      },
      {
        Effect    = "Allow"
        Action    = ["iam:CreateServiceLinkedRole"]
        Resource  = "*"
        Condition = {
          StringEquals = {
            "iam:AWSServiceName" = "restore-testing.backup.amazonaws.com"
          }
        }
      }
    ]
  })
}
