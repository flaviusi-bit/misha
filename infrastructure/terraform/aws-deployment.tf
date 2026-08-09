data "aws_caller_identity" "current" {}

data "aws_iam_policy_document" "github_actions_assume_role" {
  statement {
    effect = "Allow"

    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values   = ["repo:${var.github_repository}:ref:refs/heads/main"]
    }
  }
}

resource "aws_iam_openid_connect_provider" "github" {
  url = "https://token.actions.githubusercontent.com"

  client_id_list = ["sts.amazonaws.com"]
}

resource "aws_iam_role" "github_actions_deploy" {
  name               = "misha-github-actions-deploy"
  assume_role_policy = data.aws_iam_policy_document.github_actions_assume_role.json
}

data "aws_iam_policy_document" "github_actions_deploy" {
  statement {
    effect = "Allow"

    actions = [
      "ecr:GetAuthorizationToken"
    ]

    resources = ["*"]
  }

  statement {
    effect = "Allow"

    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:CompleteLayerUpload",
      "ecr:InitiateLayerUpload",
      "ecr:PutImage",
      "ecr:UploadLayerPart"
    ]

    resources = [aws_ecr_repository.misha.arn]
  }

  statement {
    effect = "Allow"

    actions = [
      "s3:GetObject",
      "s3:PutObject",
      "s3:DeleteObject"
    ]

    resources = ["arn:aws:s3:::misha-terraform-state/misha/dev/terraform.tfstate"]
  }

  statement {
    effect = "Allow"

    actions = [
      "s3:ListBucket"
    ]

    resources = ["arn:aws:s3:::misha-terraform-state"]

    condition {
      test     = "StringLike"
      variable = "s3:prefix"
      values   = ["misha/dev/*"]
    }
  }
}

resource "aws_iam_role_policy" "github_actions_deploy" {
  name   = "misha-github-actions-deploy"
  role   = aws_iam_role.github_actions_deploy.id
  policy = data.aws_iam_policy_document.github_actions_deploy.json
}

resource "aws_ecr_repository" "misha" {
  name                 = "misha"
  image_tag_mutability = "IMMUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_lifecycle_policy" "misha" {
  repository = aws_ecr_repository.misha.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep the latest 20 images"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = 20
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

output "github_actions_role_arn" {
  value = aws_iam_role.github_actions_deploy.arn
}

output "ecr_repository_url" {
  value = aws_ecr_repository.misha.repository_url
}
