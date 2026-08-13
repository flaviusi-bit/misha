resource "aws_backup_vault" "application" {
  name          = "${local.name}-backup"
  force_destroy = var.environment != "prod"
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
}

resource "aws_iam_role_policy_attachment" "backup" {
  role       = aws_iam_role.backup.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForBackup"
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
