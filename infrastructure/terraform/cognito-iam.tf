resource "aws_iam_role_policy" "github_actions_cognito" {
  name = "${local.name}-github-actions-cognito"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "cognito-idp:CreateUserPool",
        "cognito-idp:DeleteUserPool",
        "cognito-idp:DescribeUserPool",
        "cognito-idp:UpdateUserPool",
        "cognito-idp:GetUserPoolMfaConfig",
        "cognito-idp:ListUserPools",
        "cognito-idp:TagResource",
        "cognito-idp:UntagResource",
        "cognito-idp:ListTagsForResource",
        "cognito-idp:CreateUserPoolDomain",
        "cognito-idp:DeleteUserPoolDomain",
        "cognito-idp:DescribeUserPoolDomain",
        "cognito-idp:CreateResourceServer",
        "cognito-idp:DeleteResourceServer",
        "cognito-idp:DescribeResourceServer",
        "cognito-idp:UpdateResourceServer",
        "cognito-idp:CreateUserPoolClient",
        "cognito-idp:DeleteUserPoolClient",
        "cognito-idp:DescribeUserPoolClient",
        "cognito-idp:UpdateUserPoolClient",
        "cognito-idp:ListUserPoolClients",
        "cognito-idp:CreateGroup",
        "cognito-idp:DeleteGroup",
        "cognito-idp:GetGroup",
        "cognito-idp:UpdateGroup"
      ]
      Resource = "*"
    }]
  })
}
