resource "aws_ecr_repository" "worker" {
  name                 = "${local.name}-worker"
  image_tag_mutability = "IMMUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  encryption_configuration {
    encryption_type = "AES256"
  }
}

resource "aws_ecr_lifecycle_policy" "worker" {
  repository = aws_ecr_repository.worker.name

  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Keep the latest 20 worker images"
      selection = {
        tagStatus   = "any"
        countType   = "imageCountMoreThan"
        countNumber = 20
      }
      action = { type = "expire" }
    }]
  })

  depends_on = [aws_iam_role_policy.github_actions_deploy]
}

# Keep worker log-group permissions independently managed from the large
# deployment policy. This guarantees the permissions required to reconcile
# the existing log group are materialized before Terraform updates it.
resource "aws_iam_role_policy" "github_actions_worker_logs" {
  name = "${local.name}-github-actions-worker-logs"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "logs:CreateLogGroup",
        "logs:DeleteLogGroup",
        "logs:DeleteRetentionPolicy",
        "logs:DescribeLogGroups",
        "logs:ListTagsForResource",
        "logs:PutRetentionPolicy",
        "logs:TagResource",
        "logs:UntagResource"
      ]
      Resource = "arn:aws:logs:${var.aws_region}:576984879588:log-group:/ecs/${local.name}/worker"
    }]
  })
}

# IAM policy writes are eventually consistent. Terraform's dependency graph
# guarantees ordering, but AWS can still reject the immediately-following
# API call while the new identity policy is propagating. Wait once after the
# policy is materialized so the log-group update is deterministic.
resource "terraform_data" "github_actions_worker_logs_propagation" {
  triggers_replace = [aws_iam_role_policy.github_actions_worker_logs.id]

  provisioner "local-exec" {
    command = "sleep 60"
  }

  depends_on = [aws_iam_role_policy.github_actions_worker_logs]
}

resource "aws_cloudwatch_log_group" "worker" {
  name              = "/ecs/${local.name}/worker"
  retention_in_days = 30

  # The dedicated worker policy must be created and allowed time to propagate
  # before Terraform operates on the existing worker log group.
  depends_on = [
    aws_iam_role_policy.github_actions_deploy,
    terraform_data.github_actions_worker_logs_propagation
  ]
}

resource "aws_iam_role" "ecs_worker_task" {
  name = "${local.name}-ecs-worker-task"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_role_policy" "ecs_worker_task" {
  name = "${local.name}-ecs-worker-task-policy"
  role = aws_iam_role.ecs_worker_task.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "sqs:SendMessage",
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:GetQueueAttributes"
      ]
      Resource = aws_sqs_queue.application_events.arn
    }]
  })
}

resource "aws_ecs_task_definition" "worker" {
  family                   = "${local.name}-worker"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.ecs_worker_cpu
  memory                   = var.ecs_worker_memory
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_worker_task.arn

  container_definitions = jsonencode([{
    name      = "worker"
    image     = var.worker_container_image != "" ? var.worker_container_image : "public.ecr.aws/docker/library/alpine:3.20"
    essential = true

    environment = [
      { name = "DB_HOST", value = aws_db_instance.postgres.address },
      { name = "DB_PORT", value = "5432" },
      { name = "DB_NAME", value = var.db_name },
      { name = "Outbox__QueueUrl", value = aws_sqs_queue.application_events.url }
    ]

    secrets = [
      { name = "DB_USER", valueFrom = "${aws_db_instance.postgres.master_user_secret[0].secret_arn}:username::" },
      { name = "DB_PASSWORD", valueFrom = "${aws_db_instance.postgres.master_user_secret[0].secret_arn}:password::" }
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.worker.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "worker"
      }
    }
  }])

  lifecycle {
    ignore_changes = [container_definitions]
  }

  depends_on = [aws_iam_role_policy.github_actions_deploy]
}

resource "aws_ecs_service" "worker" {
  name            = "${local.name}-worker"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.worker.arn
  desired_count   = var.ecs_worker_desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = [for subnet in aws_subnet.private : subnet.id]
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = false
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  lifecycle {
    ignore_changes = [task_definition]
    precondition {
      condition     = var.environment != "prod" || var.ecs_worker_desired_count >= 2
      error_message = "Production requires at least two ECS worker tasks for availability during deployments."
    }
  }

  depends_on = [aws_iam_role_policy.github_actions_deploy]
}
