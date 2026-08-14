# Bootstrap permissions are intentionally independent of worker resources.
# Terraform must apply these before it can create the worker ECR repository,
# log group, task role, and DLQ alarm. The resource-scoped worker policy below
# can then be applied after those resources exist.
resource "aws_iam_role_policy" "github_actions_worker_bootstrap" {
  name = "${local.name}-github-actions-worker-bootstrap"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ecr:CreateRepository",
          "ecr:TagResource"
        ]
        Resource = "arn:aws:ecr:${var.aws_region}:576984879588:repository/${local.name}-worker"
      },
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:TagResource"
        ]
        Resource = "arn:aws:logs:${var.aws_region}:576984879588:log-group:/ecs/${local.name}/worker"
      },
      {
        Effect = "Allow"
        Action = [
          "iam:GetRole",
          "iam:CreateRole",
          "iam:DeleteRole",
          "iam:UpdateAssumeRolePolicy",
          "iam:PutRolePolicy",
          "iam:DeleteRolePolicy",
          "iam:GetRolePolicy",
          "iam:ListRolePolicies",
          "iam:TagRole",
          "iam:UntagRole",
          "iam:PassRole"
        ]
        Resource = "arn:aws:iam::576984879588:role/${local.name}-ecs-worker-task"
      },
      {
        Effect = "Allow"
        Action = [
          "cloudwatch:PutMetricAlarm",
          "cloudwatch:DeleteAlarms",
          "cloudwatch:DescribeAlarms",
          "cloudwatch:TagResource",
          "cloudwatch:UntagResource"
        ]
        Resource = "arn:aws:cloudwatch:${var.aws_region}:576984879588:alarm:${local.name}-application-events-dlq-depth"
      }
    ]
  })
}

resource "aws_iam_role_policy" "github_actions_worker_deploy" {
  name = "${local.name}-github-actions-worker-deploy"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:CompleteLayerUpload",
          "ecr:DeleteLifecyclePolicy",
          "ecr:DescribeRepositories",
          "ecr:GetLifecyclePolicy",
          "ecr:InitiateLayerUpload",
          "ecr:ListTagsForResource",
          "ecr:PutImage",
          "ecr:PutImageScanningConfiguration",
          "ecr:PutImageTagMutability",
          "ecr:PutLifecyclePolicy",
          "ecr:TagResource",
          "ecr:UntagResource",
          "ecr:UploadLayerPart"
        ]
        Resource = aws_ecr_repository.worker.arn
      },
      {
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "logs:ListTagsForResource",
          "logs:CreateLogGroup",
          "logs:DeleteLogGroup",
          "logs:PutRetentionPolicy",
          "logs:DeleteRetentionPolicy",
          "logs:TagResource",
          "logs:UntagResource"
        ]
        Resource = "${aws_cloudwatch_log_group.worker.arn}:*"
      }
    ]
  })
}
