resource "terraform_data" "iam_policy_propagation" {
  triggers_replace = [
    sha256(aws_iam_role_policy.github_actions_deploy.policy),
    sha256(aws_iam_role_policy.github_actions_backup_restore_testing.policy),
  ]

  depends_on = [
    aws_iam_role_policy.github_actions_deploy,
    aws_iam_role_policy.github_actions_backup_restore_testing,
  ]

  provisioner "local-exec" {
    command = "sleep 20"
  }
}

resource "aws_backup_vault" "application" {
  name          = "${local.name}-backup"
  force_destroy = var.environment != "prod"

  depends_on = [terraform_data.iam_policy_propagation]
}

resource "aws_iam_role" "backup" {
  name = "${local.name}-backup"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "backup.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  depends_on = [terraform_data.iam_policy_propagation]
}

resource "aws_iam_role_policy_attachment" "backup" {
  role       = aws_iam_role.backup.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForBackup"
}

resource "aws_iam_role_policy_attachment" "restore" {
  role       = aws_iam_role.backup.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForRestores"
}

resource "aws_backup_plan" "application" {
  name = "${local.name}-backup-plan"

  rule {
    rule_name         = "daily"
    target_vault_name = aws_backup_vault.application.name
    schedule          = "cron(0 2 * * ? *)"
    start_window      = 60
    completion_window = 180

    lifecycle {
      delete_after = var.environment == "prod" ? 35 : 7
    }
  }

  tags = {
    Environment = var.environment
    ManagedBy   = "terraform"
    Purpose     = "application-recovery"
  }
}

resource "aws_backup_selection" "postgres" {
  iam_role_arn = aws_iam_role.backup.arn
  name         = "${local.name}-postgres"
  plan_id      = aws_backup_plan.application.id
  resources    = [aws_db_instance.postgres.arn]
}

resource "aws_backup_restore_testing_plan" "application" {
  name = "${replace(local.name, "-", "_")}_restore_test"

  recovery_point_selection {
    algorithm             = "LATEST_WITHIN_WINDOW"
    include_vaults        = [aws_backup_vault.application.arn]
    recovery_point_types  = ["SNAPSHOT"]
    selection_window_days = 7
  }

  schedule_expression = "cron(0 4 ? * SUN *)"
  start_window_hours  = 4

  depends_on = [terraform_data.iam_policy_propagation]
}

resource "aws_backup_restore_testing_selection" "postgres" {
  name                      = "postgres"
  restore_testing_plan_name = aws_backup_restore_testing_plan.application.name
  protected_resource_type   = "RDS"
  protected_resource_arns   = [aws_db_instance.postgres.arn]
  iam_role_arn              = aws_iam_role.backup.arn
  validation_window_hours   = 2

  depends_on = [
    aws_iam_role_policy_attachment.restore,
    terraform_data.iam_policy_propagation,
  ]
}
