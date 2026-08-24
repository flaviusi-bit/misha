resource "aws_iam_role_policy" "github_actions_backup_permissions" {
  name = "${local.name}-github-actions-backup-permissions"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "AllowBackupStorageMountCapsule"
        Effect   = "Allow"
        Action   = ["backup-storage:MountCapsule"]
        Resource = "*"
      },
      {
        Sid    = "AllowBackupVaultKmsOperations"
        Effect = "Allow"
        Action = [
          "kms:CreateGrant",
          "kms:DescribeKey",
          "kms:RetireGrant",
          "kms:Decrypt",
          "kms:GenerateDataKey"
        ]
        Resource = "*"
        Condition = {
          StringEquals = {
            "kms:ViaService" = "backup.eu-central-1.amazonaws.com"
          }
        }
      },
      {
        Sid    = "AllowBackupVerificationRead"
        Effect = "Allow"
        Action = [
          "backup:DescribeBackupVault",
          "backup:ListBackupPlans",
          "backup:ListBackupSelections",
          "backup:ListRecoveryPointsByBackupVault",
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
        Sid      = "AllowRestoreTestingServiceLinkedRole"
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
