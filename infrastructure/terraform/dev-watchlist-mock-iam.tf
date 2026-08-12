resource "aws_iam_role_policy" "github_actions_watchlist_mock_ecr" {
  name = "${local.name}-github-actions-watchlist-mock-ecr"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "ecr:BatchCheckLayerAvailability",
        "ecr:CompleteLayerUpload",
        "ecr:DescribeRepositories",
        "ecr:GetDownloadUrlForLayer",
        "ecr:InitiateLayerUpload",
        "ecr:ListImages",
        "ecr:PutImage",
        "ecr:UploadLayerPart"
      ]
      Resource = aws_ecr_repository.watchlist_mock[0].arn
    }]
  })
}
